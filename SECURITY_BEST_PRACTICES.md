# Mejores Prácticas de Seguridad - Facturador

## 📋 Tabla de Contenidos

1. [Seguridad de Secretos](#seguridad-de-secretos)
2. [Seguridad de Certificados](#seguridad-de-certificados)
3. [Seguridad de la Imagen Docker](#seguridad-de-la-imagen-docker)
4. [Seguridad de Red](#seguridad-de-red)
5. [Seguridad de Base de Datos](#seguridad-de-base-de-datos)
6. [Seguridad de API](#seguridad-de-api)
7. [Logging y Auditoría](#logging-y-auditoría)
8. [Escaneo de Vulnerabilidades](#escaneo-de-vulnerabilidades)

---

## Seguridad de Secretos

### ❌ NUNCA HAGAS

```dockerfile
# ❌ INCORRECTO - Secretos embedidos en Dockerfile
COPY .env .
RUN echo "API_KEY=mi_clave_secreta" >> config.env
RUN docker build --build-arg PASSWORD=mysecret123
```

```bash
# ❌ INCORRECTO - Secretos en commit
git add .env.production.local
git commit -m "Add passwords"
git push
```

```docker-compose
# ❌ INCORRECTO - Hardcoded passwords
environment:
  MYSQL_PASSWORD: "ProduccionSegura2025!"
  API_KEY: "tuclaveenbase64"
```

### ✅ SIEMPRE HAZ

```dockerfile
# ✅ CORRECTO - Sin secretos en Dockerfile
# Imagen limpia y portable
```

```bash
# ✅ CORRECTO - Secretos en .env (NO versionado)
cat .gitignore
# .env*
# .env.local
# .env.production.local
```

```docker-compose
# ✅ CORRECTO - Variables de entorno
environment:
  MYSQL_PASSWORD: ${MYSQL_PROD_PASSWORD}  # De .env.production.local
  API_KEY: ${API_KEY}
```

### Gestión de Secretos por Ambiente

**Local Development:**
```bash
# .env.local (NO versionado)
# Solo valores de ejemplo, NO reales
MYSQL_PROD_PASSWORD=localpassword123
API_KEY=localkey456
```

**AWS Production:**
```bash
# AWS Secrets Manager o Parameter Store
# ✓ Encriptado en reposo (KMS)
# ✓ Encriptado en tránsito (HTTPS/TLS)
# ✓ Auditoría de acceso (CloudTrail)
# ✓ Rotación automática
# ✓ Versionado
```

### Prácticas de Rotación

```bash
# 1. Plan de rotación trimestral (mínimo)
# 2. Crear nuevo secret en Secrets Manager
# 3. Actualizar .env en EC2
# 4. Restart container (health checks previenen downtime)
# 5. Verificar logs que funciona
# 6. Documentar cambio en log de auditoría
# 7. Destruir valor anterior (después de 30 días de gracia)
```

---

## Seguridad de Certificados

### Validación de Certificados

```bash
# Verificar autenticidad
openssl x509 -in facturador-produccion_290c16f6d34b67bd.crt -text -noout | grep -A2 "Subject:"

# Verificar fecha de vencimiento
openssl x509 -enddate -noout -in facturador-produccion_290c16f6d34b67bd.crt
# notAfter=Oct  1 04:16:40 2026 GMT

# Verificar fingerprint (comparar con AFIP)
openssl x509 -fingerprint -noout -in facturador-produccion_290c16f6d34b67bd.crt

# Verificar que la clave privada corresponde al certificado
openssl x509 -noout -modulus -in facturador-produccion_290c16f6d34b67bd.crt | openssl md5
openssl rsa -noout -modulus -in facturador-produccion.key | openssl md5
# Deben coincidir
```

### Permisos de Archivos

```bash
# Certificados deben tener permisos restrictivos
chmod 400 facturador-produccion_290c16f6d34b67bd.crt
chmod 400 facturador-produccion.key

# Owner debe ser usuario de aplicación
chown 1000:1000 facturador-produccion_290c16f6d34b67bd.crt
chown 1000:1000 facturador-produccion.key

# Verify:
ls -la facturador-produccion*
# Esperado:
# -r-------- 1 appuser appuser  ... facturador-produccion_290c16f6d34b67bd.crt
# -r-------- 1 appuser appuser  ... facturador-produccion.key
```

### Ciclo de Vida de Certificado

```
┌─────────────────────────────────────────────────┐
│ Certificado ARCA Válido hasta: 2026-10-01       │
├─────────────────────────────────────────────────┤
│                                                  │
│ Hoy                                 Vencimiento  │
│ (2025-09-30)  ←─────────────────→  (2026-10-01) │
│   ✓                                              │
│   354 días restantes                            │
│                                                  │
│   Acción requerida a: 2026-08-01                │
│   (60 días antes)                               │
│                                                  │
│   CRÍTICO: 2026-09-15 (14 días antes)           │
│   URGENCIA: Actualizar o párada total           │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Monitoreo de Vencimiento

```bash
# Script de verificación (cron diario)
#!/bin/bash

CERT="/run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt"
DAYS_WARNING=60
DAYS_CRITICAL=14

expiration_date=$(openssl x509 -enddate -noout -in "$CERT" | cut -d= -f2)
expiration_epoch=$(date -d "$expiration_date" +%s)
now_epoch=$(date +%s)
days_remaining=$(( ($expiration_epoch - $now_epoch) / 86400 ))

if [ "$days_remaining" -lt "$DAYS_CRITICAL" ]; then
    echo "CRÍTICO: Certificado vence en $days_remaining días"
    # Enviar alerta a PagerDuty, Slack, etc.
elif [ "$days_remaining" -lt "$DAYS_WARNING" ]; then
    echo "ADVERTENCIA: Certificado vence en $days_remaining días"
    # Preparar renovación
fi
```

---

## Seguridad de la Imagen Docker

### Docker Scan (Vulnerabilidades)

```bash
# Escanear imagen
docker scan facturador-net:latest

# O con Trivy (recomendado)
trivy image facturador-net:latest

# Salida esperada:
# ✓ No vulnerabilidades críticas
# ✓ Algunas vulnerabilidades bajas OK
# ❌ Cualquier "CRITICAL" o "HIGH" = corregir
```

### Best Practices de Dockerfile

```dockerfile
# ✅ CORRECTO
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Usar usuario no-root
RUN useradd -m -u 1000 appuser
USER appuser

# No instalar herramientas de debug en producción
# No incluir archivos de construcción

# Health check
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "facturador.net.dll"]
```

### Signaturas de Imagen (Opcional)

```bash
# Usar Docker Content Trust para firmar imágenes
export DOCKER_CONTENT_TRUST=1

# Build y push (requiere credenciales de firma)
docker build -t registry.example.com/facturador-net:latest .
docker push registry.example.com/facturador-net:latest

# Pull verifica firma automáticamente
docker pull registry.example.com/facturador-net:latest
```

### Registry Privado (AWS ECR)

```bash
# ✅ Mejor que Docker Hub para producción

# Crear repositorio
aws ecr create-repository --repository-name facturador-net --region us-east-1

# Login
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com

# Push
docker tag facturador-net:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/facturador-net:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/facturador-net:latest

# Política de ciclo de vida (eliminar imágenes viejas)
# ECR → Repositories → Life cycle rules
```

---

## Seguridad de Red

### VPC Segmentation

```
┌─────────────────────────────────────────────┐
│ AWS VPC 10.0.0.0/16                         │
├─────────────────────────────────────────────┤
│                                              │
│  ┌──────────────────────────────────────┐  │
│  │ Public Subnet 10.0.1.0/24            │  │
│  ├──────────────────────────────────────┤  │
│  │ • ALB (Application Load Balancer)    │  │
│  │   - Certificates (ACM)               │  │
│  │   - HTTPS (443)                      │  │
│  │   - HTTP → HTTPS redirect (80)       │  │
│  │ • NAT Gateway (para EC2 outbound)    │  │
│  └──────────────────────────────────────┘  │
│           ↓                                  │
│  ┌──────────────────────────────────────┐  │
│  │ Private Subnet 10.0.2.0/24           │  │
│  ├──────────────────────────────────────┤  │
│  │ • EC2 (facturador-net)               │  │
│  │   - NO public IP                     │  │
│  │   - Only receives from ALB           │  │
│  │ • Docker containers                  │  │
│  │   - MySQL (internal only)            │  │
│  │   - facturador-net (:8080)           │  │
│  │                                      │  │
│  │ • No incoming traffic from Internet  │  │
│  └──────────────────────────────────────┘  │
│                                              │
└─────────────────────────────────────────────┘
```

### Security Groups

**ALB Security Group:**
```
Inbound:
  - HTTPS (443) from 0.0.0.0/0
  - HTTP (80) from 0.0.0.0/0
Outbound:
  - All to EC2 SG on port 8080

EC2 Security Group:
Inbound:
  - Port 8080 from ALB SG
  - Port 22 (SSH) from [your-ip]/32 (admin only)
Outbound:
  - All (para descargar dependencias, conectar WSAA)
```

### TLS/HTTPS

```bash
# Certificados en ALB (AWS Certificate Manager - FREE)
# 1. Request certificate en ACM
# 2. Validate ownership (DNS CNAME)
# 3. Attach a ALB listener HTTPS
# 4. ALB → EC2 puede ser HTTP (ALB termina TLS)

# Verify:
curl -v https://facturador.example.com/health
# Expected: HTTP 200, Certificate valid
```

---

## Seguridad de Base de Datos

### Credenciales

```bash
# ❌ NUNCA
database.password=ProduccionSegura2025!

# ✅ SIEMPRE
database.password=${MYSQL_PROD_PASSWORD}
# Valor viene de AWS Secrets Manager
```

### Permisos

```bash
# User facturador_prod_user solo puede:
# - SELECT, INSERT, UPDATE, DELETE en facturador_prod
# - NO acceso a otras bases de datos
# - NO permisos DROP, ALTER

# Crear usuario con permisos limitados:
GRANT SELECT, INSERT, UPDATE, DELETE ON facturador_prod.* 
  TO 'facturador_prod_user'@'%' 
  IDENTIFIED BY '${MYSQL_PROD_PASSWORD}';

# NO root access desde aplicación
```

### Acceso a Base de Datos

```bash
# ❌ NO expongas MySQL a Internet
# ports:
#   - "3306:3306"  # ❌ ELIMINAR

# ✅ Solo acceso desde Docker network (interno)
```

### Backups

```bash
# Daily automated backups
0 2 * * * docker exec facturador-mysql-prod mysqldump \
  -u root -p${MYSQL_PROD_ROOT_PASSWORD} \
  facturador_prod | gzip > /backups/facturador_$(date +\%Y\%m\%d).sql.gz

# Store backups en:
# - AWS S3 (Lifecycle policy: delete after 30 days)
# - AWS RDS Snapshots (automático si usas RDS)

# Test restore anualmente
```

---

## Seguridad de API

### Autenticación

**API Key Header (Actual):**
```bash
# ✓ Requerido en todas las requests
# ✓ Enviado en header X-API-KEY (no en URL)

curl -H "X-API-KEY: ${API_KEY}" http://api/endpoint

# ✅ Mejor: cambiar a JWT Bearer tokens (ya está configurado)
curl -H "Authorization: Bearer ${TOKEN}" http://api/endpoint
```

### Rate Limiting (Implementar)

```csharp
// Agregar en Program.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
    {
        options.PermitLimit = 100;              // 100 requests
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });
});

app.UseRateLimiter();
```

### CORS

```csharp
// ✅ CORRECTO - Solo dominios permitidos
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>();

// allowedOrigins:
// - http://127.0.0.1:5175 (local dev)
// - https://facturador.example.com (prod)
```

### Input Validation

```csharp
// ✅ Validar TODOS los inputs
[ApiController]
[Route("api/[controller]")]
public class PacientesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PacienteDTO dto)
    {
        // Validación automática por atributos
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        // Validación adicional
        if (string.IsNullOrWhiteSpace(dto.NombrePaciente))
            return BadRequest("Nombre es requerido");
        
        // ... resto del código
    }
}
```

### Error Handling

```csharp
// ❌ NUNCA expongas stack traces en producción
throw new Exception("SQL Error: " + ex.StackTrace);

// ✅ Log internamente, retorna error genérico
_logger.LogError(ex, "Error processing request");
return StatusCode(500, new { error = "Internal server error" });
```

---

## Logging y Auditoría

### Qué Loguear

✅ **SÍ**:
```
- ARCA authentication attempts (success/failure)
- API requests (endpoint, user, timestamp)
- Database operations (CREATE, UPDATE, DELETE)
- Errors y excepciones
- Security events (auth failures, permission denied)
- System events (startup, shutdown, configuration)
```

❌ **NO**:
```
- Passwords o API keys
- Números de tarjeta
- PII personal sensitive (ver GDPR/CCPA)
- Tokens JWT completos (solo hash)
- Stack traces en producción
```

### Configuración de Serilog

```csharp
// Program.cs - Serilog ya configurado ✓
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 10,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10485760
    )
    .Enrich.FromLogContext()
    .CreateLogger();
```

### Enviar Logs a CloudWatch

```bash
# En EC2, configure Docker daemon:
# /etc/docker/daemon.json
{
  "log-driver": "awslogs",
  "log-opts": {
    "awslogs-group": "/aws/ec2/facturador",
    "awslogs-region": "us-east-1",
    "awslogs-stream-prefix": "docker"
  }
}

# Restart Docker:
sudo systemctl restart docker
```

### Auditoría de Cambios (Opcional)

```csharp
// Agregar auditoría en DbContext
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is IAuditable)
        .ToList();

    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            ((IAuditable)entry.Entity).CreatedAt = DateTime.UtcNow;
        }
        if (entry.State == EntityState.Modified)
        {
            ((IAuditable)entry.Entity).ModifiedAt = DateTime.UtcNow;
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

---

## Escaneo de Vulnerabilidades

### Dependency Scanning (NuGet)

```bash
# Escanear dependencias .NET
dotnet list package --deprecated
dotnet list package --outdated
dotnet list package --vulnerable

# Actualizar paquetes
dotnet package update --outdated
```

### OWASP Top 10

```
Verificar la aplicación contra:
☐ A01:2021 Broken Access Control → ver AuthenticationMiddleware
☐ A02:2021 Cryptographic Failures → ver AES encryption
☐ A03:2021 Injection → ORM previene SQL injection
☐ A04:2021 Insecure Design → ver validaciones
☐ A05:2021 Security Misconfiguration → ver .env
☐ A06:2021 Vulnerable Components → ver nuget packages
☐ A07:2021 Identification/Authentication → ver JWT
☐ A08:2021 Software/Data Integrity → ver signatures
☐ A09:2021 Logging/Monitoring → ver Serilog
☐ A10:2021 Server-Side Request Forgery → validated URLs
```

### Herramientas de Análisis

```bash
# OWASP Dependency-Check
dependency-check --project "facturador-net" \
  --scan c:\Users\Usuario\Desktop\facturadorLau\facturadorApi

# SonarQube (análisis de código)
sonar-scanner \
  -Dsonar.projectKey=facturador-net \
  -Dsonar.sources=facturadorApi
```

---

## Checklist de Seguridad Pre-Production

- [ ] Dockerfile no contiene secretos
- [ ] .env* archivos en .gitignore
- [ ] docker-compose passwords: ${VAR}
- [ ] Certificados ARCA en /run/secrets/arca/
- [ ] Permisos de certificado: chmod 400
- [ ] Base de datos: contraseña fuerte (>16 chars)
- [ ] API Key: única y compleja
- [ ] JWT Secret: >32 caracteres
- [ ] Encryption Key: 32 caracteres
- [ ] TLS/HTTPS configurado en ALB
- [ ] MySQL sin acceso desde internet
- [ ] EC2 sin port 22 abierto al mundo (SSH solo desde bastion)
- [ ] docker scan: cero vulnerabilidades críticas
- [ ] Logs no contienen secretos
- [ ] Rate limiting implementado
- [ ] CORS configurado para dominios permitidos
- [ ] Input validation en todos los endpoints
- [ ] Error handling: sin stack traces expuestos
- [ ] AWS Secrets Manager / Parameter Store configurado
- [ ] Backups automáticos en S3
- [ ] CloudWatch logs configurado
- [ ] IAM roles con least privilege
- [ ] Network segmentation validado
- [ ] Certificate monitoring (vencimiento)
- [ ] Security group rules reviewed
- [ ] Prueba de penetración (opcional pero recomendado)

---

## Recursos Adicionales

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [AWS Security Best Practices](https://aws.amazon.com/security/best-practices/)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework/)
- [CIS Benchmarks](https://www.cisecurity.org/)

---

**Última actualización**: 2025-09-30  
**Responsable**: DevOps / Security Team  
**Revisión recomendada**: Semestral
