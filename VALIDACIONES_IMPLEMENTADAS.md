# Validaciones de Entrada de Datos - facturador.net

## Resumen de cambios

Se implementó un sistema completo de validaciones para todos los campos de ingreso de datos en la API. Las validaciones se realizan tanto a nivel de datos (Data Annotations) como lógicamente en los DTOs.

---

## 1. **Validadores Personalizados** (Archivo: `Extensions/ValidationAttributes.cs`)

Se crearon 5 validadores personalizados reutilizables:

### 🔹 `[Dni]` - Validador de DNI
- **Requisito**: Exactamente 8 dígitos
- **Formato aceptado**: `^\d{8}$`
- **Mensaje de error**: "El {campo} debe contener exactamente 8 dígitos."
- **Uso**: En campos de DNI

### 🔹 `[Cuit]` - Validador de CUIT
- **Requisito**: Exactamente 11 dígitos
- **Formato aceptado**: `^\d{11}$`
- **Mensaje de error**: "El {campo} debe contener exactamente 11 dígitos."
- **Uso**: En campos de CUIT

### 🔹 `[LettersOnly]` - Validador de Nombres/Apellidos
- **Requisito**: Solo letras (incluyendo tildes), espacios y guiones
- **Formato aceptado**: `^[a-záéíóúñàèìòùäëïöüA-ZÁÉÍÓÚÑÀÈÌÒÙÄËÏÖÜ\s\-]+$`
- **Rechaza**: Números y caracteres especiales
- **Mensaje de error**: "El {campo} solo puede contener letras, espacios y guiones (sin números ni caracteres especiales)."
- **Uso**: Nombres, apellidos, nombre de pacientes, nombre de receptores

### 🔹 `[EmailValidation]` - Validador de Email
- **Requisito**: Formato válido de email
- **Formato aceptado**: `^[^\s@]+@[^\s@]+\.[^\s@]+$`
- **Mensaje de error**: "El {campo} debe ser una dirección de correo válida."
- **Uso**: Campos de email (opcional, puede ser nulo)

### 🔹 `[NumAfiliado]` - Validador de Número de Afiliado
- **Requisito**: Números, letras y guiones
- **Formato aceptado**: `^[a-zA-Z0-9\-]+$`
- **Mensaje de error**: "El {campo} contiene caracteres inválidos. Solo se permiten números, letras y guiones."
- **Uso**: Números de afiliado

---

## 2. **Validaciones en PacienteDTO** (Archivo: `DTOs/PacienteDTO.cs`)

| Campo | Validación | Regla |
|-------|-----------|-------|
| **DNI** | `[Required]` `[Dni]` | Obligatorio, exactamente 8 dígitos |
| **NumAfiliado** | `[Required]` `[NumAfiliado]` `[StringLength(50)]` | Obligatorio, 1-50 caracteres |
| **Nombre** | `[Required]` `[LettersOnly]` `[StringLength(100)]` | Obligatorio, solo letras, 2-100 caracteres |
| **Apellido** | `[Required]` `[LettersOnly]` `[StringLength(100)]` | Obligatorio, solo letras, 2-100 caracteres |
| **Domicilio** | `[Required]` `[StringLength(200)]` | Obligatorio, 5-200 caracteres |
| **PlanillaAsistenciaImagenUrl** | `[Url]` `[StringLength(500)]` | URL válida (opcional), máx 500 caracteres |
| **Observaciones** | `[StringLength(500)]` | Máx 500 caracteres |

---

## 3. **Validaciones en ObraSocialDTO** (Archivo: `DTOs/ObraSocialDTO.cs`)

| Campo | Validación | Regla |
|-------|-----------|-------|
| **CUIT** | `[Required]` `[Cuit]` | Obligatorio, exactamente 11 dígitos |
| **Nombre** | `[Required]` `[LettersOnly]` `[StringLength(150)]` | Obligatorio, solo letras, 2-150 caracteres |
| **DomicilioComercial** | `[Required]` `[StringLength(200)]` | Obligatorio, 5-200 caracteres |
| **Condicion** | `[Required]` | Obligatorio (enum) |
| **Mail** | `[EmailValidation]` `[StringLength(150)]` | Email válido (opcional), máx 150 caracteres |

---

## 4. **Validaciones en ArcaSolicitudCaeDTO** (Archivo: `DTOs/ArcaSolicitudCaeDTO.cs`)

| Campo | Validación | Regla |
|-------|-----------|-------|
| **NombreReceptor** | `[LettersOnly]` `[StringLength(150)]` | Solo letras, máx 150 caracteres |
| **DomicilioReceptor** | `[StringLength(200)]` | Máx 200 caracteres |
| **ImporteTotal** | `[Range(0.01, double.MaxValue)]` | Mayor a 0.01 |
| **PacienteNombre** | `[LettersOnly]` `[StringLength(150)]` | Solo letras, máx 150 caracteres |
| **PacienteDni** | `[Dni]` `[StringLength(20)]` | 8 dígitos (opcional) |
| **PacienteNumAfiliado** | `[NumAfiliado]` `[StringLength(20)]` | Alfanumérico con guiones (opcional) |
| **DescripcionServicio** | `[StringLength(500)]` | Máx 500 caracteres |
| **CantidadSesiones** | `[Range(1, int.MaxValue)]` | Mayor a 0 (opcional) |
| **PrecioSesion** | `[Range(0.01, double.MaxValue)]` | Mayor a 0.01 (opcional) |

**Validaciones lógicas adicionales** (en método `Validate()`):
- Si es facturación no manual: requiere cantidad de sesiones y precio válidos
- Importe total debe coincidir con (cantidad × precio de sesión)
- Fecha de inicio no puede ser posterior a fecha de fin

---

## 5. **Cómo funcionan las validaciones**

### En el Backend (API)
Las validaciones se ejecutan automáticamente cuando:
1. Se recibe una solicitud HTTP con un DTO
2. ASP.NET Core valida los atributos de Data Annotations
3. Si hay errores, retorna HTTP 400 (Bad Request) con detalles

**Ejemplo de respuesta de error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Dni": ["El DNI debe contener exactamente 8 dígitos."],
    "Nombre": ["El nombre solo puede contener letras, espacios y guiones."]
  }
}
```

### En el Frontend (UI - Recomendado)
Se recomienda agregar validaciones correspondientes en Vue.js para mejorar UX. Ejemplos:
```javascript
// DNI: 8 números
dni: [
  v => /^\d{8}$/.test(v) || 'El DNI debe contener exactamente 8 dígitos'
]

// CUIT: 11 números
cuit: [
  v => /^\d{11}$/.test(v) || 'El CUIT debe contener exactamente 11 dígitos'
]

// Nombre/Apellido: sin números
nombre: [
  v => /^[a-záéíóúñ\s\-]+$/i.test(v) || 'No se permiten números en nombres'
]

// Email
email: [
  v => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) || 'Email inválido'
]
```

---

## 6. **Campos validados por endpoint**

### POST/PUT /api/pacientes
✅ DNI: 8 números  
✅ NumAfiliado: 1-50 caracteres, alfanumérico con guiones  
✅ Nombre: 2-100 caracteres, solo letras  
✅ Apellido: 2-100 caracteres, solo letras  
✅ Domicilio: 5-200 caracteres  
✅ URL de imagen: URL válida si se proporciona  

### POST/PUT /api/obras-sociales
✅ CUIT: 11 números exactos  
✅ Nombre: 2-150 caracteres, solo letras  
✅ Domicilio: 5-200 caracteres  
✅ Mail: Email válido si se proporciona  

### POST /api/arca/solicitud-cae
✅ Nombre del receptor: solo letras si se proporciona  
✅ DNI del paciente: 8 números si se proporciona  
✅ Número de afiliado: alfanumérico si se proporciona  
✅ Importe total: > 0  
✅ Cantidad de sesiones: > 0 si no es manual  
✅ Precio de sesión: > 0 si no es manual  
✅ Consistencia de importe total con cálculo de sesiones  

---

## 7. **Próximos pasos recomendados**

1. ✅ **Implementar validaciones frontend** en `facturadorUi/src` para mejorar UX
2. ✅ **Agregar tests unitarios** para validadores personalizados
3. ✅ **Documentar en Swagger/OpenAPI** los formatos esperados
4. ✅ **Logs de auditoría** para intentos de datos inválidos

---

## Archivos modificados

```
✏️  Extensions/ValidationAttributes.cs         (CREADO - Validadores personalizados)
✏️  DTOs/PacienteDTO.cs                        (MODIFICADO - Validaciones añadidas)
✏️  DTOs/ObraSocialDTO.cs                      (MODIFICADO - Validaciones añadidas)
✏️  DTOs/ArcaSolicitudCaeDTO.cs                (MODIFICADO - Validaciones añadidas)
```

**Estado**: ✅ Compilación exitosa, lista para uso
