# facturador.net

API REST en .NET 8 para la gestión y facturación de una práctica psicopedagógica.

## Tecnologías
- **.NET 8** + ASP.NET Core
- **MySQL 8** via Pomelo EF Core
- **JWT Bearer** para autenticación
- **Serilog** para logging estructurado con rotación diaria
- **Swagger / OpenAPI**
- **Docker** + docker-compose

## Estructura

```
facturador.net/
├── conf/               # appsettings, AuthRole.json, EncryptionSettings
├── Controllers/        # Controladores REST
├── Data/               # DbContext, SeedData, Converters, Constraints
├── DTOs/               # Objetos de transferencia de datos
├── Extensions/         # ServiceCollectionExtensions (DI auto-registro)
├── Middleware/         # ExceptionMiddleware, AuthenticationMiddleware
├── Migrations/         # EF Core migrations
├── Models/             # Entidades del dominio
├── Repositories/       # IRepository<T>, Repository<T>, repos específicos
├── Services/           # IXService, XServices (auto-registrados por reflection)
├── Utils/              # Filters, EncryptionHelper, DecimalJsonConverter
└── logs/               # Logs diarios (excluir de git)
```

## Primeros pasos

1. Configurar la cadena de conexión en `conf/appsettings.json` con la base `facturadordb`
2. Configurar `Jwt:SecretKey`, `Encryption:Key/IV` y `ApiSettings:ApiKey`
3. Crear la migración inicial: `dotnet ef migrations add Inicial`
4. Aplicar la migración: `dotnet ef database update`
5. Ejecutar: `dotnet run`

## Docker

```bash
docker-compose up --build
```

## Convenciones

- **Repositorios**: heredar `Repository<T>` e implementar `IXRepository : IRepository<T>`
- **Services**: implementar `IXService`, el nombre debe terminar en `Service` o `Services` para el auto-registro
- **Capitalización**: todos los strings se capitalizan automáticamente en la DB (excepto Password, Email, UserName, Notas)
- **AuthRole.json**: define qué roles pueden acceder a cada action de cada controller
- **ARCA**: las credenciales y certificados se conservan fuera del repositorio y del contexto de Docker.
