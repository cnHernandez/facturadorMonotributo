# Estrategia de Deployment en AWS EC2

## 📋 Tabla de Contenidos

1. [Arquitectura General](#arquitectura-general)
2. [Preparación de AWS](#preparación-de-aws)
3. [Deployment Local (Testing)](#deployment-local-testing)
4. [Migration a AWS EC2](#migration-a-aws-ec2)
5. [Gestión de Secretos](#gestión-de-secretos)
6. [Base de Datos](#base-de-datos)
7. [Certificados ARCA](#certificados-arca)
8. [CI/CD Pipeline](#cicd-pipeline)
9. [Monitoreo y Logs](#monitoreo-y-logs)
10. [Troubleshooting](#troubleshooting)

---

## Arquitectura General

### Arquitectura Local (Docker Compose)
```
┌─────────────────────────────────────────────────────────────┐
│ Host Local (Windows/Linux/Mac)                              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Docker Desktop                                       │  │
│  ├──────────────────────────────────────────────────────┤  │
│  │                                                       │  │
│  │  ┌─────────────────┐    ┌──────────────────────────┐│  │
│  │  │  facturador-net │    │ database-prod (MySQL)   ││  │
│  │  │ (ASP.NET Core)  │───→│ :3306 (internal)       ││  │
│  │  │ :8080 internal  │    └──────────────────────────┘│  │
│  │  └─────────────────┘           │                    │  │
│  │         │                       │                    │  │
│  │         └───────────────────────┘                    │  │
│  │                 │                                    │  │
│  │  ┌──────────────────────────────────────────────┐  │  │
│  │  │ Volumes (Read-Only Mounts)                  │  │  │
│  │  ├──────────────────────────────────────────────┤  │  │
│  │  │ • /run/secrets/arca/ ← Certificates ✓       │  │  │
│  │  │ • ./logs/ ← Application logs                │  │  │
│  │  │ • ./.runtime/ ← ARCA ticket cache           │  │  │
│  │  └──────────────────────────────────────────────┘  │  │
│  │                                                       │  │
│  └──────────────────────────────────────────────────────┘  │
│         ↑                              ↑                     │
│    :8081 (Exposed)              :3308 (Debug only)         │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Arquitectura AWS EC2 (Meta)
```
┌─────────────────────────────────────────────────────────────────────┐
│ AWS Account                                                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │ VPC (Private Network)                                      │    │
│  ├────────────────────────────────────────────────────────────┤    │
│  │                                                            │    │
│  │  ┌──────────────────────────────┐  Public Subnet          │    │
│  │  │  Application Load Balancer   │  (ALB)                  │    │
│  │  │  • HTTPS/TLS (:443)          │                         │    │
│  │  │  • Certificates (ACM)        │                         │    │
│  │  └──────────┬───────────────────┘                         │    │
│  │             │                                             │    │
│  │  ┌──────────┴─────────────┐                               │    │
│  │  │ EC2 Instance           │  Private Subnet              │    │
│  │  ├──────────────────────────┤  (No public IP)            │    │
│  │  │ • Docker Engine          │                            │    │
│  │  │ • Docker Compose         │                            │    │
│  │  │ • facturador-net (:8080) │                            │    │
│  │  │ • MySQL (:3306, internal)│                            │    │
│  │  └──────────┬───────────────┘                             │    │
│  │             │                                             │    │
│  │  ┌──────────▼──────────────────────┐                      │    │
│  │  │ Secrets Manager / Parameter     │                      │    │
│  │  │ Store                           │                      │    │
│  │  ├──────────────────────────────────┤                     │    │
│  │  │ • ARCA Certificates             │                     │    │
│  │  │ • DB Password                   │                     │    │
│  │  │ • API Keys                      │                     │    │
│  │  │ • JWT Secrets                   │                     │    │
│  │  └──────────────────────────────────┘                     │    │
│  │                                                            │    │
│  │  ┌──────────────────────────────────┐                     │    │
│  │  │ CloudWatch                       │                     │    │
│  │  ├──────────────────────────────────┤                     │    │
│  │  │ • Application logs               │                     │    │
│  │  │ • Container logs                 │                     │    │
│  │  │ • Metrics & Alarms               │                     │    │
│  │  └──────────────────────────────────┘                     │    │
│  │                                                            │    │
│  │  (Optional) ┌──────────────────────┐                      │    │
│  │             │ RDS MySQL (Future)   │                      │    │
│  │             │ • Multi-AZ           │                      │    │
│  │             │ • Automated Backups  │                      │    │
│  │             └──────────────────────┘                      │    │
│  │                                                            │    │
│  └────────────────────────────────────────────────────────────┘    │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Preparación de AWS

### 1. Crear AWS Account y Configurar Acceso

```bash
# Instalar AWS CLI
# Windows (PowerShell Admin):
msiexec.exe /i https://awscli.amazonaws.com/AWSCLIV2.msi

# macOS:
brew install awscli

# Linux:
sudo apt-get install awscli
```

### 2. Configurar Credenciales de AWS

```bash
aws configure

# Ingresar:
# AWS Access Key ID: ****
# AWS Secret Access Key: ****
# Default region: us-east-1 (o tu región preferida)
# Default output format: json
```

### 3. Crear VPC y Subnets (Recomendado)

```bash
# Opción 1: Usar AWS Console (más fácil)
# Opción 2: Usar AWS CloudFormation o Terraform

# Valores recomendados:
# - VPC CIDR: 10.0.0.0/16
# - Public Subnet: 10.0.1.0/24
# - Private Subnet: 10.0.2.0/24
```

### 4. Crear Security Groups

**Para ALB (Application Load Balancer):**
- Inbound: HTTPS (443), HTTP (80)
- Source: 0.0.0.0/0

**Para EC2 Instance:**
- Inbound: SSH (22), Custom (8080) from ALB security group
- Source: ALB SG, Admin IP for SSH

---

## Deployment Local (Testing)

### Paso 1: Preparar Variables de Entorno

```bash
# Navegar al directorio raíz del proyecto
cd c:\Users\Usuario\Desktop\facturadorLau

# El archivo .env.production.local DEBE tener:
cat .env.production.local
```

### Paso 2: Validar Certificados Locales

```bash
# Verificar que los certificados existen en la raíz del proyecto
ls -la facturador-produccion*

# Output esperado:
# facturador-produccion.key
# facturador-produccion_290c16f6d34b67bd.crt
```

### Paso 3: Construir y Ejecutar Local

```bash
cd facturadorApi

# Build de la imagen (sin certificados embedidos)
docker build -t facturador-net:latest .

# Ejecutar con docker-compose
docker-compose --env-file ..\.env.production.local -f docker-compose.production.yml up -d

# Verificar estado
docker-compose -f docker-compose.production.yml ps

# Ver logs
docker-compose -f docker-compose.production.yml logs -f facturador-net
```

### Paso 4: Pruebas de Validación Local

```bash
# 1. API Health Check
curl -X GET http://127.0.0.1:8081/health

# 2. ARCA Authentication
curl -X POST http://127.0.0.1:8081/api/arca/autenticacion \
  -H "X-API-KEY: tuclaveenbase64" \
  -H "Content-Type: application/json" \
  -d '{}'

# 3. Obtener Último Comprobante
curl -X GET http://127.0.0.1:8081/api/arca/ultimo-comprobante/1 \
  -H "X-API-KEY: tuclaveenbase64"

# 4. Database Connection
curl -X GET http://127.0.0.1:8081/health/db

# 5. Frontend
# Open browser: http://127.0.0.1:5175
```

---

## Migration a AWS EC2

### Fase 1: Preparación de Infraestructura

#### 1.1 Crear EC2 Instance

```bash
# Requisitos mínimos:
# - t3.small (2 vCPU, 2 GB RAM) para producción
# - Ubuntu 22.04 LTS (HVM)
# - Storage: 50 GB (gp3)
# - Security Group: Configured per above

# En AWS Console:
# EC2 → Instances → Launch Instance
# Nombre: facturador-prod
# AMI: Ubuntu 22.04 LTS
# Instance Type: t3.small
# Key Pair: crear y descargar
```

#### 1.2 Conectar a la Instancia

```bash
# SSH a la instancia
ssh -i /path/to/key.pem ubuntu@<instance-public-ip>

# (Nota: En VPC privada, usar Systems Manager Session Manager o Bastion Host)
```

### Fase 2: Instalar Docker en EC2

```bash
# Update system
sudo apt-get update
sudo apt-get upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add ubuntu user to docker group
sudo usermod -aG docker ubuntu

# Logout and login again, or:
newgrp docker

# Verify installation
docker --version
docker run hello-world

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
docker-compose --version
```

### Fase 3: Copiar Archivos a EC2

```bash
# Desde tu máquina local:

# 1. Crear directorio en EC2
ssh -i key.pem ubuntu@<ip> 'mkdir -p ~/facturador'

# 2. Copiar código fuente (sin secretos)
scp -i key.pem -r facturadorApi/ ubuntu@<ip>:~/facturador/
scp -i key.pem -r facturadorUi/ ubuntu@<ip>:~/facturador/
scp -i key.pem docker-compose.production.yml ubuntu@<ip>:~/facturador/

# 3. Copiar .env (SOLO VÍA SSH SEGURO, NUNCA SCP)
# Crear el archivo directamente en EC2:
ssh -i key.pem ubuntu@<ip> 'cat > ~/facturador/.env.production.local << EOF
TZ=America/Argentina/Buenos_Aires
MYSQL_PROD_ROOT_PASSWORD=NewSecurePassword123!
MYSQL_PROD_USER=facturador_prod_user
MYSQL_PROD_PASSWORD=UserPassword456!
MYSQL_PROD_DATABASE=facturador_prod
JWT_SECRET_KEY=NewJWTSecretWith32Characters123!
ENCRYPTION_KEY=NewEncryptionKey1234567890123456
ENCRYPTION_IV=NewVectorStart0!
API_KEY=NewSecureApiKey789!
ARCA_CUIT=27190124415
ARCA_PUNTO_VENTA=4
ARCA_WSAA_URL=https://wsaa.afip.gov.ar/ws/services/LoginCms
ARCA_WSFE_URL=https://servicios1.afip.gov.ar/wsfev1/service.asmx
ARCA_CERT_PATH=/run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt
ARCA_PRIVATE_KEY_PATH=/run/secrets/arca/facturador-produccion.key
EOF'
```

### Fase 4: Gestión de Secretos (CRÍTICO)

#### Opción A: Secrets Manager (Recomendado para Producción)

```bash
# En AWS Console:
# Secrets Manager → Store a new secret

# JSON Secret para ARCA Certificates:
{
  "certificate": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----",
  "privateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
}

# Retrieve en EC2:
aws secretsmanager get-secret-value --secret-id arca-certificates-prod --query SecretString --output text | jq -r '.certificate' > /run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt
aws secretsmanager get-secret-value --secret-id arca-certificates-prod --query SecretString --output text | jq -r '.privateKey' > /run/secrets/arca/facturador-produccion.key
chmod 400 /run/secrets/arca/*
```

#### Opción B: Parameter Store

```bash
# Store parameter:
aws ssm put-parameter \
  --name /facturador/arca-cert \
  --value file://facturador-produccion_290c16f6d34b67bd.crt \
  --type SecureString

# Retrieve:
aws ssm get-parameter \
  --name /facturador/arca-cert \
  --with-decryption \
  --query 'Parameter.Value' \
  --output text > /run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt
```

#### Opción C: Manual (Menos seguro, solo para testing)

```bash
# En EC2, crear directorio:
sudo mkdir -p /run/secrets/arca
sudo chmod 700 /run/secrets/arca

# Copiar certificados (NUNCA vía internet sin encripción):
# Usar Systems Manager Session Manager o copiar encrypted vía SFTP

# Set permissions:
sudo chmod 400 /run/secrets/arca/*
sudo chown 1000:1000 /run/secrets/arca/*
```

### Fase 5: Deployment con Docker Compose

```bash
# En EC2:
cd ~/facturador

# Build imagen
docker build -t facturador-net:latest -f facturadorApi/Dockerfile .

# Iniciar servicios
docker-compose --env-file .env.production.local -f docker-compose.production.yml up -d

# Verificar
docker-compose -f docker-compose.production.yml ps
docker-compose -f docker-compose.production.yml logs -f facturador-net
```

### Fase 6: Configurar Load Balancer (ALB)

```bash
# En AWS Console:
# EC2 → Load Balancers → Create Application Load Balancer

# Configuración:
# - Name: facturador-alb
# - Scheme: internet-facing
# - VPC: Tu VPC
# - Subnets: Public subnets (múltiples para HA)
# - Security Group: ALB SG (puerto 443, 80)

# Listeners:
# - HTTP (80) → Redirect to HTTPS
# - HTTPS (443) → Target Group → EC2 (8080)

# Certificates:
# - Use AWS Certificate Manager (ACM)
# - Request certificate para tu dominio
# - Attach a ALB
```

---

## Gestión de Secretos

### Variables de Entorno en AWS

Cada variable puede venir de:

1. **AWS Secrets Manager**: Valores sensibles (passwords, keys)
2. **AWS Parameter Store**: Configuración no-sensible
3. **Docker .env**: Local development only
4. **EC2 IAM Role**: Credenciales de AWS

### Estructura de Secretos Recomendada

```
Secrets Manager:
├── /prod/database/password
├── /prod/arca/certificates
├── /prod/jwt/secret-key
├── /prod/encryption/key
└── /prod/api/key

Parameter Store:
├── /prod/database/host
├── /prod/database/user
├── /prod/database/name
├── /prod/arca/cuit
├── /prod/arca/urls
└── /prod/timezone
```

### Rotación de Secretos

```bash
# 1. Crear nuevo valor en Secrets Manager
# 2. Actualizar container con nuevo valor
# 3. Validar que funciona
# 4. Actualizar endpoint de la aplicación

# Automation (opcional):
# - Lambda function para rotación automática
# - Lambda ejecuta: actualizar secret → restart container
```

---

## Base de Datos

### Opción 1: Docker MySQL en EC2 (Actual)

✅ Ventajas:
- Mismo setup que local
- Fácil de entender
- Bajo costo inicial

❌ Desventajas:
- No hay multi-AZ
- Backups manuales
- No hay automatic scaling

**Uso continuado:**
```bash
# En EC2, MySQL persiste en volumen Docker
docker volume ls
docker volume inspect facturador_mysql_prod_data

# Backups manuales:
docker exec facturador-mysql-prod mysqldump \
  -u facturador_prod_user -pPassword \
  facturador_prod > backup.sql

# Restore:
docker exec -i facturador-mysql-prod mysql \
  -u facturador_prod_user -pPassword \
  facturador_prod < backup.sql
```

### Opción 2: AWS RDS MySQL (Futuro - Producción Mayor)

🔄 **Migration Path:**

```
Fase 1: Docker MySQL en EC2 (Hoy)
   ↓
Fase 2: RDS MySQL Multi-AZ (Cuando escales)
   ↓
Fase 3: Aurora MySQL (Cuando necesites HA/DR)
```

**Cuando migrar a RDS:**
1. Tráfico > 1000 req/día
2. Datos críticos que no pueden perderse
3. Necesidad de backups automáticos
4. Multi-AZ redundancy

**Pasos para migración a RDS:**
```bash
# 1. Crear RDS MySQL Multi-AZ
# 2. Crear base de datos vacía
# 3. Dump desde Docker MySQL
# 4. Restore a RDS
# 5. Actualizar connection string en .env
# 6. Pruebas de validación
# 7. Switchover (cambiar DNS)
```

---

## Certificados ARCA

### Ciclo de Vida de Certificados

```
┌─────────────┐
│ Certificado │  Válido 1-2 años
│ ARCA        │  Renovable antes de vencer
└──────┬──────┘
       │
       ├─→ ¿Vence en 30 días?
       │   └─→ Solicitar renovación a AFIP
       │
       ├─→ ¿Vence en 1 día?
       │   └─→ URGENTE: Actualizar en Secrets Manager
       │
       └─→ ¿Expirado?
           └─→ CRÍTICO: PARADA DE PRODUCCIÓN
               Requerir certificado de emergencia a AFIP
```

### Actualizar Certificados en Producción

```bash
# 1. Obtener nuevo certificado de AFIP

# 2. Subir a Secrets Manager
aws secretsmanager update-secret \
  --secret-id arca-certificates-prod \
  --secret-string file://new-secret.json

# 3. En EC2, descargar nuevo certificado
aws secretsmanager get-secret-value \
  --secret-id arca-certificates-prod \
  --query SecretString --output text | jq -r '.certificate' > \
  /run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt

# 4. Restart container (sin downtime con health checks)
docker-compose restart facturador-net

# 5. Verificar
curl -X POST http://127.0.0.1:8081/api/arca/autenticacion \
  -H "X-API-KEY: tuclaveenbase64" \
  -H "Content-Type: application/json" \
  -d '{}'

# Expected: HTTP 200 + Token válido
```

---

## CI/CD Pipeline

### GitHub Actions (Recomendado)

```yaml
# .github/workflows/deploy-to-aws.yml
name: Deploy to AWS EC2

on:
  push:
    branches: [main, production]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1
      
      - name: Build and push image
        run: |
          docker build -t facturador-net:${{ github.sha }} facturadorApi/
          docker tag facturador-net:${{ github.sha }} facturador-net:latest
          # Push to ECR
          aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin ${{ secrets.ECR_REGISTRY }}
          docker push ${{ secrets.ECR_REGISTRY }}/facturador-net:latest
      
      - name: Deploy to EC2
        env:
          EC2_HOST: ${{ secrets.EC2_HOST }}
          EC2_USER: ubuntu
          EC2_KEY: ${{ secrets.EC2_PRIVATE_KEY }}
        run: |
          echo "$EC2_KEY" > key.pem
          chmod 600 key.pem
          ssh -i key.pem $EC2_USER@$EC2_HOST "cd ~/facturador && docker pull ${{ secrets.ECR_REGISTRY }}/facturador-net:latest && docker-compose restart"
```

### AWS CodePipeline (Alternativa)

```
Source (GitHub) 
  ↓
Build (CodeBuild)
  ├─ Build Docker image
  ├─ Run tests
  └─ Push to ECR
  ↓
Deploy (CodeDeploy)
  └─ Deploy to EC2
```

---

## Monitoreo y Logs

### CloudWatch Logs

```bash
# En EC2, configurar Docker daemon para enviar logs a CloudWatch
# /etc/docker/daemon.json:
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

### CloudWatch Metrics

```bash
# Crear alarms para:
# - CPU > 80%
# - Memory > 85%
# - Disk > 90%
# - Container exited unexpectedly
# - Database connection failures
```

### Application Insights

```bash
# En AWS Console:
# Application Insights → Create Application
# Select EC2 instance
# Monitor: Application logs, System logs, Custom metrics
```

---

## Troubleshooting

### Problema 1: Docker no inicia

```bash
# Logs:
docker-compose -f docker-compose.production.yml logs facturador-net

# Causas comunes:
# 1. Certificados no encontrados:
#    → Verificar /run/secrets/arca/ existe
#    → Verificar permisos (chmod 400)
#
# 2. Base de datos no accesible:
#    → docker-compose -f docker-compose.production.yml logs database-prod
#    → Verificar password en .env.production.local
#
# 3. Puerto en uso:
#    → sudo netstat -tulpn | grep 8080
#    → docker-compose down (antes de subir)
```

### Problema 2: ARCA Authentication falla

```bash
# Verificar certificado:
openssl x509 -in /run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt -text -noout

# Verificar clave privada:
openssl rsa -in /run/secrets/arca/facturador-produccion.key -check -noout

# Probar conexión a WSAA:
curl -v https://wsaa.afip.gov.ar/ws/services/LoginCms

# Logs de la app:
docker-compose -f docker-compose.production.yml logs facturador-net | grep -i arca
```

### Problema 3: Base de datos llenadose

```bash
# Check tamaño:
docker exec facturador-mysql-prod du -sh /var/lib/mysql

# Cleanup:
docker exec facturador-mysql-prod mysql -u root -pPassword << EOF
PURGE BINARY LOGS BEFORE DATE_SUB(NOW(), INTERVAL 7 DAY);
OPTIMIZE TABLE facturador_prod.*;
EOF
```

### Problema 4: Certificados cercanos a vencer

```bash
# Verificar fecha de vencimiento:
openssl x509 -enddate -noout -in /run/secrets/arca/facturador-produccion_290c16f6d34b67bd.crt

# Automatizar alerta (cron job):
# 0 0 * * * /usr/local/bin/check-cert-expiry.sh
```

---

## Checklist Final de Deployment

- [ ] Docker instalado y funcionando en EC2
- [ ] Source code copiado (sin .git, sin secretos)
- [ ] .env.production.local creado con valores reales
- [ ] Certificados ARCA en /run/secrets/arca/
- [ ] Base de datos inicializada
- [ ] docker-compose build sin errores
- [ ] docker-compose up -d sin errores
- [ ] Health check endpoint responde
- [ ] ARCA authentication funciona
- [ ] Frontend carga correctamente
- [ ] ALB configurado y apuntando a EC2
- [ ] HTTPS funciona a través de ALB
- [ ] CloudWatch logs configurados
- [ ] CloudWatch alarms configuradas
- [ ] Backups automáticos configurados (si usa RDS)
- [ ] Secrets Manager configurado con rotación automática
- [ ] CI/CD pipeline configurado
- [ ] IAM roles y policies asignados correctamente
- [ ] VPC security groups validados
- [ ] Pruebas de carga realizadas

---

## Resumen: Migración Rápida (TL;DR)

```bash
# Local:
cd facturadorLau
docker-compose --env-file .env.production.local -f facturadorApi/docker-compose.production.yml up -d
curl http://127.0.0.1:8081/health

# AWS EC2:
ssh -i key.pem ubuntu@<instance-ip>
cd ~/facturador
docker-compose --env-file .env.production.local -f docker-compose.production.yml up -d
curl http://localhost:8080/health

# Cambiar DNS en tu dominio para apuntar a ALB
# ✓ Migración completada
```

---

## Contacto y Soporte

Para dudas sobre:
- **ARCA Integration**: AFIP - https://www.afip.gob.ar/
- **AWS Infrastructure**: AWS Support - https://console.aws.amazon.com/support/
- **Docker**: Docker Documentation - https://docs.docker.com/

---

**Última actualización**: 2025-09-30  
**Estado**: Production Ready ✓  
**Confidencialidad**: Documento interno - NO compartir con clientes
