# Checklist de Validación Final - Docker + Producción Local + AWS Portabilidad

**Fecha de Preparación**: 2025-09-30  
**Estado**: ✓ Ready for Testing  
**Ambiente**: Producción Local (Docker Compose) + AWS EC2 Ready  

---

## 📋 REQUISITOS DE VALIDACIÓN (15 puntos)

### Grupo 1: Certificados ARCA en Docker Image

#### ✓ VALIDACIÓN 1: Certificados NO embedidos en Dockerfile
**Requisito**: La imagen Docker NUNCA debe contener certificados embedidos
**Archivo**: `facturadorApi/Dockerfile`

```bash
# Test: Verificar que Dockerfile NO copia certificados
grep -i "COPY.*\.key\|COPY.*\.crt" facturadorApi/Dockerfile
# Resultado esperado: (sin output = ✓ PASS)

# Test: Verificar historia de imagen (no contiene secretos)
docker inspect facturador-net:latest | grep -i "certificat\|secret\|password"
# Resultado esperado: (sin output = ✓ PASS)

# Test: Verificar docker history
docker history facturador-net:latest | grep -i "secret\|password\|certificate"
# Resultado esperado: (sin output = ✓ PASS)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

#### ✓ VALIDACIÓN 2: Paths de certificados en appsettings.Production.json
**Requisito**: Los paths de certificados deben ser `/run/secrets/arca/...` y respetarán variables de entorno

**Archivo**: `facturadorApi/conf/appsettings.Production.json`

```bash
# Test: Verificar appsettings.Production.json
cat facturadorApi/conf/appsettings.Production.json | grep -A2 "CertificatePath\|PrivateKeyPath"
# Resultado esperado:
# "CertificatePath": "/run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt",
# "PrivateKeyPath": "/run/secrets/arca/facturador-produccion.key"

# Test: Verificar que NO contiene paths locales tipo "facturador-produccion.key"
grep -E "CertificatePath.*[^/]facturador-produccion" facturadorApi/conf/appsettings.Production.json
# Resultado esperado: (sin output = ✓ PASS)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 2: Producción Local Funcional

#### ✓ VALIDACIÓN 3: Producción local funciona sin cambios en Development
**Requisito**: Development NO se ve afectado por cambios de Producción

**Archivo**: `facturadorApi/conf/appsettings.Development.json` (sin cambios)

```bash
# Test: Ejecutar en Development
cd facturadorApi
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Verificar que inicia sin errores
# Resultado esperado: Application started. Press Ctrl+C to shut down.

# Test: API responde
curl http://127.0.0.1:5000/health

# Resultado esperado: HTTP 200 OK
```

**Estado**: [ ] PASS  [ ] FAIL  

---

#### ✓ VALIDACIÓN 4: Database Production usa facturador_prod schema SOLO
**Requisito**: Connection string en Producción apunta EXCLUSIVAMENTE a `facturador_prod`, NUNCA a development

**Archivo**: `facturadorApi/conf/appsettings.Production.json` + `.env.production.local`

```bash
# Test: Verificar connection string
cat facturadorApi/conf/appsettings.Production.json | grep -i "database"
# Output debe contener: "facturador_prod"
# Output NO debe contener: "facturadordb", "dev", "test"

# Test: Verificar en docker-compose 
grep "facturador_prod" facturadorApi/docker-compose.production.yml
# Resultado esperado: MYSQL_DATABASE: ${MYSQL_PROD_DATABASE:-facturador_prod}

# Test: Ejecutar contenedor y verificar
docker-compose -f facturadorApi/docker-compose.production.yml exec database-prod mysql -u root -pPassword -e "SHOW DATABASES;" | grep facturador
# Resultado esperado: 
# facturador_prod
# (NO debe haber facturador_dev, facturadordb, etc.)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 3: Dockerfile Security

#### ✓ VALIDACIÓN 5: Dockerfile multi-stage sin secretos
**Requisito**: Dockerfile usa multi-stage, no contiene `COPY *.key`, `COPY *.crt`, `RUN echo PASSWORD=`

**Archivo**: `facturadorApi/Dockerfile`

```bash
# Test: Contar etapas
grep "^FROM" facturadorApi/Dockerfile | wc -l
# Resultado esperado: 2 (build-env y runtime)

# Test: Verificar sin COPY de secretos
grep "COPY.*\.\(key\|crt\|pem\|p12\)" facturadorApi/Dockerfile
# Resultado esperado: (sin output = ✓ PASS)

# Test: Verificar sin RUN echo de secrets
grep "RUN echo.*PASSWORD\|RUN echo.*KEY\|RUN echo.*SECRET" facturadorApi/Dockerfile
# Resultado esperado: (sin output = ✓ PASS)

# Test: Verificar que runtime stage solo copia build output
grep -A20 "^FROM.*aspnet" facturadorApi/Dockerfile | grep "COPY --from=build-env"
# Resultado esperado: COPY --from=build-env /app/out .
```

**Estado**: [ ] PASS  [ ] FAIL  

---

#### ✓ VALIDACIÓN 6: .dockerignore contiene `*.key`, `*.crt`, `.env*`
**Requisito**: Archivo `.dockerignore` explícitamente excluye secretos

**Archivo**: `.dockerignore` (en raíz del proyecto)

```bash
# Test: Archivo existe
ls -la .dockerignore
# Resultado esperado: -rw-r--r-- 1 usuario grupo ...

# Test: Contiene exclusiones de secretos
grep -E "\\*\\.key|\\*\\.crt|\\*\\.pem|\\.env" .dockerignore
# Resultado esperado:
# *.key
# *.crt
# .env*

# Test: Contiene exclusiones de otros artefactos
grep "bin/\|obj/\|logs/" .dockerignore
# Resultado esperado: bin/, obj/, logs/
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 4: Variables de Entorno

#### ✓ VALIDACIÓN 7: docker-compose.production.yml usa variables de entorno
**Requisito**: Todos los valores sensibles usan `${VARIABLE_NAME}`, NO hardcoded

**Archivo**: `facturadorApi/docker-compose.production.yml`

```bash
# Test: Verificar MYSQL_ROOT_PASSWORD
grep "MYSQL_ROOT_PASSWORD" facturadorApi/docker-compose.production.yml
# Resultado esperado: MYSQL_ROOT_PASSWORD: ${MYSQL_PROD_ROOT_PASSWORD}
# (contiene ${ } = variable)

# Test: Verificar MYSQL_PASSWORD
grep "MYSQL_PASSWORD:" facturadorApi/docker-compose.production.yml
# Resultado esperado: MYSQL_PASSWORD: ${MYSQL_PROD_PASSWORD}

# Test: Verificar CONNECTION STRING
grep "ConnectionStrings__DefaultConnection" facturadorApi/docker-compose.production.yml
# Resultado esperado: utiliza ${MYSQL_PROD_PASSWORD}, no hardcoded

# Test: Verificar ARCA configuration
grep "Arca__CertificatePath\|Arca__PrivateKeyPath" facturadorApi/docker-compose.production.yml
# Resultado esperado: ${ARCA_CERT_PATH}, ${ARCA_PRIVATE_KEY_PATH}
```

**Estado**: [ ] PASS  [ ] FAIL  

---

#### ✓ VALIDACIÓN 8: .env.production.local existe con template completo
**Requisito**: Archivo `.env.production.local` contiene todas las variables necesarias (aunque sea con valores de ejemplo)

**Archivo**: `.env.production.local` (en raíz del proyecto)

```bash
# Test: Archivo existe
ls -la .env.production.local
# Resultado esperado: -rw-r--r-- 1 usuario grupo ...

# Test: Contiene todas las variables requeridas
for VAR in MYSQL_PROD_ROOT_PASSWORD MYSQL_PROD_USER MYSQL_PROD_PASSWORD JWT_SECRET_KEY ENCRYPTION_KEY ENCRYPTION_IV API_KEY ARCA_CUIT ARCA_PUNTO_VENTA ARCA_WSAA_URL ARCA_WSFE_URL ARCA_CERT_PATH ARCA_PRIVATE_KEY_PATH; do
    grep "^${VAR}=" .env.production.local
done
# Resultado esperado: Todas las variables definidas

# Test: .env* está en .gitignore
grep -E "^\\.env" .gitignore
# Resultado esperado: .env*

# Test: El archivo NO fue commiteado (no en git history)
git log --all --full-history -- ".env.production.local" | grep "commit"
# Resultado esperado: (sin output = ✓ PASS - nunca fue commiteado)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 5: MySQL Configuration

#### ✓ VALIDACIÓN 9: MySQL password en .env, NO en appsettings.json
**Requisito**: `appsettings.Production.json` contiene `Password=` vacío o placeholder, valor real viene de env

**Archivo**: `facturadorApi/conf/appsettings.Production.json`

```bash
# Test: Verificar connection string en appsettings
grep "Password=" facturadorApi/conf/appsettings.Production.json
# Resultado esperado: "Password=;" o "Password=;" (vacío o sin valor sensible)
# Resultado INCORRECTO: "Password=ProduccionSegura2025!"

# Test: Verificar que docker-compose SOBRESCRIBE
grep -A1 "ConnectionStrings__DefaultConnection" facturadorApi/docker-compose.production.yml
# Resultado esperado: ConnectionStrings__DefaultConnection: "Server=database-prod;...;Password=${MYSQL_PROD_PASSWORD}..."

# Test: En docker history, NO debe verse password real
docker history facturador-net:latest | grep -i "password.*[A-Za-z0-9]"
# Resultado esperado: (sin output = ✓ PASS)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 6: Health Checks

#### ✓ VALIDACIÓN 10: API Health Check endpoint `/health`
**Requisito**: Endpoint `/health` responde HTTP 200 cuando servicios están OK

**Archivo**: `facturadorApi/Controllers/HealthCheckController.cs`

```bash
# Test: Verificar controller existe
ls facturadorApi/Controllers/HealthCheckController.cs
# Resultado esperado: (archivo existe)

# Test: Iniciar containers en producción
cd facturadorApi
docker-compose --env-file ../.env.production.local -f docker-compose.production.yml up -d
docker-compose -f docker-compose.production.yml logs facturador-net

# Esperar ~10 segundos para startup

# Test: Llamar endpoint
curl -v http://127.0.0.1:8081/health
# Resultado esperado:
# HTTP/1.1 200 OK
# Content-Type: application/json
# {"status":"Healthy"} (o similar)

# Test: Health check de base de datos
curl -v http://127.0.0.1:8081/health/db
# Resultado esperado: HTTP 200 OK (si DB está sana)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

#### ✓ VALIDACIÓN 11: MySQL responde a health checks
**Requisito**: docker-compose health check para MySQL usa credenciales correctas

**Archivo**: `facturadorApi/docker-compose.production.yml`

```bash
# Test: Verificar health check config
grep -A5 "healthcheck:" facturadorApi/docker-compose.production.yml | grep -A5 "database-prod" -B5
# Resultado esperado:
# healthcheck:
#   test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u${MYSQL_PROD_USER}", "-p${MYSQL_PROD_PASSWORD}"]

# Test: Ver estado de health check en ejecución
docker-compose -f facturadorApi/docker-compose.production.yml ps
# Resultado esperado:
# facturador-mysql-prod   healthy
# facturador-net          running (depends_on healthy)

# Test: Conectar directamente a MySQL
docker-compose -f facturadorApi/docker-compose.production.yml exec database-prod mysql -u${MYSQL_PROD_USER} -p${MYSQL_PROD_PASSWORD} -e "SELECT 1"
# Resultado esperado: 1 (connection successful)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 7: Startup Validation

#### ✓ VALIDACIÓN 12: Program.cs valida certificados en Production startup
**Requisito**: Program.cs contiene validación que falla si certificados no existen

**Archivo**: `facturadorApi/Program.cs` (líneas ~200+)

```bash
# Test: Verificar código de validación existe
grep -A10 "PRODUCTION STARTUP VALIDATION\|ValidateOptions\|certificat" facturadorApi/Program.cs | head -20
# Resultado esperado: (debe contener lógica de validación)

# Test: Verificar archivo de certificado
grep -B5 -A5 "File.Exists.*CertificatePath\|File.Exists.*PrivateKeyPath" facturadorApi/Program.cs
# Resultado esperado: (debe validar que archivos existen)

# Test: Iniciar container SIN certificados montados (debe fallar)
cd facturadorApi
docker build -t test-validation .
docker run -e ASPNETCORE_ENVIRONMENT=Production test-validation 2>&1 | grep -i "certificat\|secret\|fail"
# Resultado esperado: (debe contener error sobre certificados faltantes)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 8: Docker Volumes & Secrets

#### ✓ VALIDACIÓN 13: Certificados ARCA montados como read-only volumes
**Requisito**: docker-compose.production.yml monta certificados con flag `:ro`

**Archivo**: `facturadorApi/docker-compose.production.yml`

```bash
# Test: Verificar volumes section
grep -A5 "volumes:" facturadorApi/docker-compose.production.yml | grep -E "arca.*:ro|certificat.*:ro"
# Resultado esperado:
# - ../facturador-produccion_290c16f6d34b67bd.crt:/run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt:ro
# - ../facturador-produccion.key:/run/secrets/arca/facturador-produccion.key:ro

# Test: Ejecutar contenedor y verificar permisos
docker-compose -f docker-compose.production.yml up -d
docker-compose -f docker-compose.production.yml exec facturador-net ls -la /run/secrets/arca/
# Resultado esperado:
# -r-------- 1 root root ... facturador-produccion_290c16f6d34b67bd.crt
# -r-------- 1 root root ... facturador-produccion.key

# Test: Intentar escribir en certificado (debe fallar)
docker-compose -f docker-compose.production.yml exec facturador-net bash -c "echo 'test' >> /run/secrets/arca/facturador-produccion.key"
# Resultado esperado: Read-only file system error
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 9: Frontend Configuration

#### ✓ VALIDACIÓN 14: Frontend vite.config.ts lee VITE_API_URL
**Requisito**: vite.config.ts usa `process.env.VITE_API_URL` con fallback

**Archivo**: `facturadorUi/vite.config.ts`

```bash
# Test: Verificar configuración
grep -A5 "target:" facturadorUi/vite.config.ts
# Resultado esperado: target: process.env.VITE_API_URL || 'http://127.0.0.1:8081',

# Test: Iniciar frontend con variable de entorno personalizada
cd facturadorUi
VITE_API_URL=http://backend.example.com npm run dev

# Verificar logs (debe mostrar que proxy apunta a backend.example.com)
# Resultado esperado: VITE_API_URL usado en proxy configuration

# Test: Iniciar sin variable (debe usar default)
npm run dev
# Resultado esperado: proxy apunta a http://127.0.0.1:8081
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### Grupo 10: AWS Deployment Documentation

#### ✓ VALIDACIÓN 15: DEPLOY_AWS.md existe y contiene estrategia completa
**Requisito**: Archivo DEPLOY_AWS.md documenta deployment en AWS EC2 sin implementación

**Archivo**: `DEPLOY_AWS.md` (en raíz del proyecto)

```bash
# Test: Archivo existe
ls -la DEPLOY_AWS.md
# Resultado esperado: -rw-r--r-- 1 usuario grupo ...

# Test: Contiene secciones clave
for SECTION in "Arquitectura General" "AWS EC2" "Secrets Manager" "RDS" "ALB" "CI/CD" "Troubleshooting"; do
    grep -i "$SECTION" DEPLOY_AWS.md
done
# Resultado esperado: Todas las secciones presentes

# Test: Contiene instrucciones de Docker instalación en EC2
grep -i "sudo.*docker\|apt.*docker\|curl.*docker" DEPLOY_AWS.md
# Resultado esperado: Instrucciones de instalación presentes

# Test: NO contiene comandos de deployment actual (solo documentación)
grep -v "^#" DEPLOY_AWS.md | grep -i "deploy.*production\|push.*main" | wc -l
# Resultado esperado: <5 (solo referencias, no ejecución)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

## 🔐 VALIDACIONES DE SEGURIDAD ADICIONALES

### ✓ SEGURIDAD 1: .env.production.local NO está en Git
```bash
# Test: Verificar .gitignore
grep "\.env" .gitignore

# Test: Verificar que nunca fue commiteado
git log --all --full-history -- .env.production.local 2>/dev/null | wc -l
# Resultado esperado: 0 (nunca en git)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### ✓ SEGURIDAD 2: Dockerfile NO contiene passwords en docker history
```bash
# Test: Verificar docker history
docker history facturador-net:latest | grep -i "password\|secret\|apikey"
# Resultado esperado: (sin output = ✓ PASS)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### ✓ SEGURIDAD 3: docker-compose.production.yml NO contiene valores hardcoded
```bash
# Test: Buscar passwords hardcodeadas
grep -E "Password=.*[A-Za-z0-9]|API_KEY=.*[A-Za-z0-9]|SECRET_KEY=.*[A-Za-z0-9]" facturadorApi/docker-compose.production.yml
# Resultado esperado: (sin output = ✓ PASS)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

### ✓ SEGURIDAD 4: ArcaAuthenticationService valida certificados
```bash
# Test: Verificar ValidateOptions() existe
grep "ValidateOptions\|File.Exists.*Certificate" facturadorApi/Services/ArcaAuthenticationService.cs
# Resultado esperado: (método existe y valida archivos)
```

**Estado**: [ ] PASS  [ ] FAIL  

---

## ✅ RESUMEN FINAL

| # | Validación | Estado | Notas |
|---|-----------|--------|-------|
| 1 | Certificados NO en Dockerfile | [ ] | |
| 2 | Paths certificados: /run/secrets/arca/ | [ ] | |
| 3 | Development sin cambios | [ ] | |
| 4 | Database: facturador_prod ONLY | [ ] | |
| 5 | Dockerfile multi-stage | [ ] | |
| 6 | .dockerignore: *.key, *.crt, .env* | [ ] | |
| 7 | docker-compose: variables ${VAR} | [ ] | |
| 8 | .env.production.local existe | [ ] | |
| 9 | MySQL password en .env | [ ] | |
| 10 | API /health endpoint works | [ ] | |
| 11 | MySQL health check works | [ ] | |
| 12 | Program.cs valida certificados | [ ] | |
| 13 | Certificados: read-only volumes | [ ] | |
| 14 | Frontend VITE_API_URL configurable | [ ] | |
| 15 | DEPLOY_AWS.md completo | [ ] | |
| S1 | .env.production.local NOT in Git | [ ] | |
| S2 | docker history sin passwords | [ ] | |
| S3 | docker-compose sin hardcoded values | [ ] | |
| S4 | ArcaAuthenticationService valida | [ ] | |

---

## 🚀 RESULTADO FINAL

**Todos PASS?** → ✅ **READY FOR AWS DEPLOYMENT**

**Algún FAIL?** → ⚠️ **MUST FIX BEFORE AWS**

---

**Fecha de Validación**: ___________  
**Validado por**: ___________  
**Aprobado para Producción**: [ ] YES  [ ] NO  

---

## Notas Finales

- Esta lista es para validación ANTES de deployar a AWS
- Revisar cada punto manualmente
- Ejecutar tests en ambiente local primero
- NO incluir .env.production.local en repositorio
- Certificados deben estar seguros en máquina local
- Para AWS, usar Secrets Manager o Parameter Store

**Status**: ✓ Ready for Implementation
