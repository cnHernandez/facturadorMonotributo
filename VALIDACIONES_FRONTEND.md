# Validaciones en Tiempo Real - Frontend (Facturador UI)

## Resumen

Se implementó un sistema completo de validaciones en tiempo real en la interfaz de usuario (React/TypeScript). Los usuarios reciben retroalimentación inmediata mientras escriben en los formularios de pacientes y obras sociales.

---

## 1. Archivo de Validaciones (`src/validations.ts`)

Se creó un módulo reutilizable con funciones de validación que se ejecutan mientras el usuario escribe.

### Funciones de Validación

#### 🔹 `validateDni(dni: string): string | null`
**Validación**: Exactamente 8 dígitos  
**Ejemplo válido**: `12345678`  
**Mensajes de error**:
- "El DNI es requerido"
- "El DNI debe contener exactamente 8 dígitos"

#### 🔹 `validateCuit(cuit: string): string | null`
**Validación**: Exactamente 11 dígitos  
**Ejemplo válido**: `12345678901`  
**Mensajes de error**:
- "El CUIT es requerido"
- "El CUIT debe contener exactamente 11 dígitos"

#### 🔹 `validateName(name: string, fieldName: string): string | null`
**Validación**: Solo letras (incluyendo tildes), espacios y guiones  
**Rechaza**: Números, caracteres especiales  
**Ejemplo válido**: `Juan Pablo García-López`  
**Mensajes de error**:
- "{fieldName} es requerido"
- "{fieldName} debe tener al menos 2 caracteres"
- "{fieldName} solo puede contener letras, espacios y guiones (sin números)"

#### 🔹 `validateNumAfiliado(numAfiliado: string): string | null`
**Validación**: Números, letras y guiones (1-50 caracteres)  
**Ejemplo válido**: `123456-789`  
**Mensajes de error**:
- "El número de afiliado es requerido"
- "El número de afiliado solo puede contener números, letras y guiones"
- "El número de afiliado debe tener entre 1 y 50 caracteres"

#### 🔹 `validateDomicilio(domicilio: string): string | null`
**Validación**: 5-200 caracteres  
**Ejemplo válido**: `Av. Principal 123, Piso 4, Departamento A`  
**Mensajes de error**:
- "El domicilio es requerido"
- "El domicilio debe tener al menos 5 caracteres"
- "El domicilio no debe exceder 200 caracteres"

#### 🔹 `validateEmail(email: string | null): string | null`
**Validación**: Email válido (opcional)  
**Ejemplo válido**: `contacto@empresa.com`  
**Mensajes de error**:
- "El correo electrónico debe ser válido"
- "El correo no debe exceder 150 caracteres"

#### 🔹 `hasErrors(errors: Record<string, string>): boolean`
**Utilidad**: Verifica si hay errores en el objeto de errores

---

## 2. Componente Field Mejorado

El componente `Field` ahora muestra:

### Elementos visuales
- **Icono de error** (🔴): Aparece cuando hay un error
- **Mensaje de error**: Texto descriptivo del problema
- **Indicador válido** (✅): Aparece cuando el campo es válido
- **Borde rojo**: Input con error tiene borde y fondo rojos

### Props del componente
```typescript
<Field
  label="Nombre"
  error={errors.nombre}      // Mensaje de error
  isValid={isFieldValid('nombre')} // true si es válido
>
  <input ... />
</Field>
```

---

## 3. Validaciones en Formulario de Pacientes

### Campos con validación en tiempo real:
| Campo | Validación | Ejemplo |
|-------|-----------|---------|
| **Nombre** | Solo letras, 2-100 car. | "Juan Carlos" ✅ / "Juan123" ❌ |
| **Apellido** | Solo letras, 2-100 car. | "García-López" ✅ / "García2" ❌ |
| **DNI** | Exactamente 8 dígitos | "12345678" ✅ / "1234567" ❌ |
| **Nº afiliado** | 1-50 car., alfanumérico | "123456-789" ✅ / "ABC@123" ❌ |
| **Domicilio** | 5-200 caracteres | "Av. Principal 123" ✅ / "Av." ❌ |

### Comportamiento:
1. **Mientras escribe**: Se valida cada carácter y muestra errores en tiempo real
2. **Campo válido**: Aparece ✅ "Válido" al lado del label
3. **Campo con error**: Se muestra el fondo rojo y el mensaje de error
4. **Botón deshabilitado**: No permite guardar mientras hay errores

---

## 4. Validaciones en Formulario de Obras Sociales

### Campos con validación en tiempo real:
| Campo | Validación | Ejemplo |
|-------|-----------|---------|
| **Nombre** | Solo letras, 2-150 car. | "OSDE" ✅ / "OSDE2024" ❌ |
| **CUIT** | Exactamente 11 dígitos | "12345678901" ✅ / "123456789" ❌ |
| **Domicilio Comercial** | 5-200 caracteres | "Av. Corrientes 1234" ✅ / "Av." ❌ |
| **Mail** | Email válido (opcional) | "contacto@osde.com" ✅ / "contacto" ❌ |

### Comportamiento:
- Igual que en pacientes
- El email es opcional, pero si se proporciona debe ser válido
- El botón "Guardar obra social" está deshabilitado si hay errores

---

## 5. Cómo Funciona la Validación

### Paso a paso:

1. **Usuario escribe** en un campo
2. **Se captura el onChange** del input
3. **Se ejecuta la función de validación** para ese campo
4. **Se actualiza el estado de errores** con el resultado
5. **Se renderiza el mensaje** de error si existe
6. **Se desactiva el botón** si hay errores

### Ejemplo: Ingresando nombre con números

```
Usuario escribe: "Juan123"
                 ↓
validateName() detecta números
                 ↓
errors.nombre = "El nombre solo puede contener letras, espacios y guiones (sin números)"
                 ↓
Se muestra error en rojo
Botón "Guardar" deshabilitado
```

---

## 6. Integración con Backend

Las validaciones del frontend **trabajan en conjunto** con las del backend:

- **Frontend**: Validación instantánea mientras escriben (UX mejorada)
- **Backend**: Validación final antes de guardar en BD (seguridad)

**Flujo de guardado**:
```
Usuario escribe datos válidos
    ↓
Frontend valida (sin errores)
    ↓
Botón habilitado, usuario hace clic en "Guardar"
    ↓
POST/PUT a /api/pacientes o /api/obras-sociales
    ↓
Backend valida nuevamente (ValidationAttributes en DTOs)
    ↓
Se guarda en BD sin errores
    ↓
Mensaje de éxito: "Paciente creado correctamente"
```

---

## 7. Estilos y UX

### Clases CSS aplicadas:
- **Error**: `border-red-300` + fondo rojo claro (`bg-red-50`)
- **Mensaje de error**: Icono + texto rojo
- **Válido**: ✅ "Válido" en verde
- **Botón deshabilitado**: Opacidad reducida + cursor no-allowed

### Ejemplos visuales:

**Campo con error:**
```
┌─ Nombre ─────────────────────────┐
│ ┌──────────────────────────────┐ │
│ │ Juan123░░░░░░░░░░░░░░░░░░░ │ │  ← Fondo rojo
│ │ 🔴 El nombre solo puede contener letras, espacios y guiones (sin números) │
│ └──────────────────────────────┘ │
└────────────────────────────────────┘
```

**Campo válido:**
```
┌─ Nombre ░░░░░░░░░░░░ ✅ Válido ─┐
│ ┌──────────────────────────────┐ │
│ │ Juan Carlos░░░░░░░░░░░░░░░░░│ │
│ └──────────────────────────────┘ │
└────────────────────────────────────┘
```

---

## 8. Archivos Modificados

```
✏️  src/validations.ts              (CREADO - Funciones de validación)
✏️  src/App.tsx                     (MODIFICADO - Formularios mejorados)
   - PatientForm con validaciones
   - ObraForm con validaciones
   - Componente Field mejorado
✏️  src/index.css                   (MODIFICADO - Estilos para errores)
```

---

## 9. Próximos Pasos Recomendados

✅ **Completado**:
- Validaciones en tiempo real en formularios
- Mensajes de error claros
- Estados visuales (error, válido, deshabilitado)

📋 **Futuro**:
- [ ] Agregar tooltips con reglas de validación
- [ ] Animaciones de error
- [ ] Historial de cambios en pacientes/obras
- [ ] Validaciones globales al guardar

---

## 10. Testing Manual

### Para probar las validaciones:

**Pacientes:**
1. Haz clic en "Nuevo paciente"
2. Intenta escribir números en "Nombre" → Debe mostrar error
3. Escribe un DNI de 7 dígitos → Debe mostrar error
4. Rellena todos los campos correctamente → ✅ botón habilitado

**Obras Sociales:**
1. Haz clic en "Nueva obra social"
2. Intenta escribir números en "Nombre" → Debe mostrar error
3. Escribe CUIT inválido → Debe mostrar error
4. Escribe email sin @ → Debe mostrar error
5. Rellena todo correctamente → ✅ botón habilitado

---

**Estado**: ✅ Compilación exitosa, lista para usar
