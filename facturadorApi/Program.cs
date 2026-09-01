using Microsoft.EntityFrameworkCore;
using Back.Data;
using Back.Repositories;
using Back.Services;
using Back.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Back.Utils;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Back.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Back.Conf;
using QuestPDF.Infrastructure;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Configurar Serilog antes de construir la aplicación
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 5,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10485760
    )
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information("facturador.net: Inicio correctamente");

try
{
    // Zona Horaria
    TimeZoneInfo argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
    TimeZoneInfo.ClearCachedData();
    TimeZoneInfo localTimeZone = argentinaTimeZone;
    Log.Information("Zona horaria configurada correctamente {TimeZone}", localTimeZone);

    // Limpiar los proveedores de configuración predeterminados
    builder.Configuration.Sources.Clear();

    // Agregar configuración desde la carpeta "conf"
    builder.Configuration
        .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "conf"))
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    // Configurar HealthChecks
    builder.Services.AddHealthChecks()
        .AddCheck("Service", () =>
            HealthCheckResult.Healthy())
        .AddMySql(builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "MySQL",
            failureStatus: HealthStatus.Unhealthy,
            timeout: TimeSpan.FromSeconds(5));

    // Obtener la clave secreta desde appsettings.json
    var secretKey = builder.Configuration["Jwt:SecretKey"];
    var key = Encoding.UTF8.GetBytes(secretKey ?? throw new InvalidOperationException("SecretKey no encontrada en la configuración"));

    // Configurar autenticación JWT
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = "facturador_net",
                ValidAudience = "facturador_net",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

    // Lee los orígenes desde appsettings.json
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            policy =>
            {
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
    });

    builder.Services.AddAuthorization();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new NotFoundResultFilter());
        options.Filters.Add(new BadRequestResultFilter());
        options.Filters.Add<ApiKeyFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    // Configurar el DbContext con MySQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(8, 0, 40))
        ));

    // Registra IHttpClientFactory
    builder.Services.AddHttpClient();
    builder.Services.AddHttpClient("ArcaWsaa", client => 
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        // En desarrollo, ignorar validación de certificados SSL de AFIP
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
    });
    builder.Services.AddMemoryCache();

    // Registro de repositories y services
    builder.Services.AddApplicationRepository();
    builder.Services.AddServicesFromAssembly(Assembly.GetExecutingAssembly());

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "facturador.net API", Version = "v1" });

        c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "API Key debe ir en el header: X-API-KEY",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Name = "X-API-KEY",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Scheme = "ApiKeyScheme"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));
    builder.Services.Configure<ArcaOptions>(builder.Configuration.GetSection(ArcaOptions.SectionName));

    var app = builder.Build();

    // Middleware de Excepciones 500
    app.UseMiddleware<ExceptionMiddleware>();

    using (var scope = app.Services.CreateScope())
    {
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<EncryptionSettings>>();
        EncryptionHelper.Initialize(settings);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors(MyAllowSpecificOrigins);
    app.UseHttpsRedirection();
    app.UseAuthentication();

    // Middleware de Autenticación por Roles
    app.UseMiddleware<AuthenticationMiddleware>();

    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicación terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
