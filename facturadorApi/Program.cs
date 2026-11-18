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
using Back.Models;
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

    // ===== VALIDACIÓN DE SEGURIDAD: Production NO puede usar base de Development =====
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'");

    if (builder.Environment.IsProduction())
    {
        // Verificar que Production NO use base de datos de Development
        var dbNameLower = connectionString.ToLowerInvariant();
        if (dbNameLower.Contains("database=facturadordb") ||
            dbNameLower.Contains("database=dev") ||
            dbNameLower.Contains("database=test") ||
            dbNameLower.Contains("database=homolog") ||
            dbNameLower.Contains("facturadordb"))
        {
            throw new InvalidOperationException(
                "❌ CRÍTICO: Production está usando una base de datos de Development/Test/Homologación. " +
                "SEGURIDAD: Connection string DEBE contener 'facturador_prod' exclusivamente. " +
                $"Detectada: {connectionString}");
        }

        if (!dbNameLower.Contains("facturador_prod"))
        {
            throw new InvalidOperationException(
                "❌ CRÍTICO: Production DEBE usar la base de datos 'facturador_prod'. " +
                $"Connection string detectada no contiene 'facturador_prod': {connectionString}");
        }

        Log.Information("✓ Validación de Seguridad OK - Production usando base: facturador_prod");
    }
    // ===== FIN VALIDACIÓN DE SEGURIDAD =====

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

    // ===== PRODUCTION STARTUP VALIDATION =====
    if (app.Environment.IsProduction())
    {
        using (var scope = app.Services.CreateScope())
        {
            var arcaOptions = scope.ServiceProvider.GetRequiredService<IOptions<ArcaOptions>>().Value;
            
            // Validate ARCA certificates exist at startup
            Log.Information("🔍 Validating ARCA certificate configuration...");
            
            if (string.IsNullOrWhiteSpace(arcaOptions.CertificatePath))
            {
                throw new InvalidOperationException(
                    "❌ CRÍTICO: Arca:CertificatePath no está configurado. " +
                    "Configure la variable de entorno Arca__CertificatePath");
            }
            
            if (string.IsNullOrWhiteSpace(arcaOptions.PrivateKeyPath))
            {
                throw new InvalidOperationException(
                    "❌ CRÍTICO: Arca:PrivateKeyPath no está configurado. " +
                    "Configure la variable de entorno Arca__PrivateKeyPath");
            }
            
            if (!File.Exists(arcaOptions.CertificatePath))
            {
                throw new InvalidOperationException(
                    $"❌ CRÍTICO: No se encontró el certificado ARCA en: {arcaOptions.CertificatePath}\n" +
                    $"Asegúrese de que el archivo esté montado como volumen read-only en /run/secrets/arca/\n" +
                    $"Opciones:\n" +
                    $"  1. Local: Montar desde host usando Docker volume bind mount\n" +
                    $"  2. AWS EC2: Usar AWS Secrets Manager o Parameter Store\n" +
                    $"  3. Verificar permisos: chmod 400 en el archivo");
            }
            
            if (!File.Exists(arcaOptions.PrivateKeyPath))
            {
                throw new InvalidOperationException(
                    $"❌ CRÍTICO: No se encontró la clave privada ARCA en: {arcaOptions.PrivateKeyPath}\n" +
                    $"Asegúrese de que el archivo esté montado como volumen read-only en /run/secrets/arca/\n" +
                    $"Opciones:\n" +
                    $"  1. Local: Montar desde host usando Docker volume bind mount\n" +
                    $"  2. AWS EC2: Usar AWS Secrets Manager o Parameter Store\n" +
                    $"  3. Verificar permisos: chmod 400 en el archivo");
            }
            
            Log.Information("✓ ARCA certificates validated successfully");
            Log.Information($"✓ Certificate: {arcaOptions.CertificatePath}");
            Log.Information($"✓ Private Key: {arcaOptions.PrivateKeyPath}");
        }
    }
    // ===== END STARTUP VALIDATION =====

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
    
    // Health check endpoint (sin autenticación - para Docker, ALB, monitoring)
    app.MapHealthChecks("/health");
    
    app.MapControllers();

    // Auto-apply migrations in Production on startup
    if (app.Environment.IsProduction())
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            Log.Information("✓ Migraciones aplicadas exitosamente en Production");
        }
    }

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Usuarios.AnyAsync(
            item => item.UserName == "admin"))
        {
            db.Usuarios.Add(new Usuario
            {
                UserName = "admin",
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword("admin123"),
                Rol = "Admin",
                Activo = true
            });

            await db.SaveChangesAsync();
            Log.Information(
                "✓ Usuario administrativo inicial creado");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicación terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
