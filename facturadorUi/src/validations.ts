// Validaciones para formularios

export type ValidationError = {
  field: string
  message: string
}

// DNI: exactamente 8 dígitos
export const validateDni = (dni: string): string | null => {
  if (!dni) return 'El DNI es requerido'
  if (!/^\d{8}$/.test(dni.trim())) {
    return 'El DNI debe contener exactamente 8 dígitos'
  }
  return null
}

// CUIT: exactamente 11 dígitos
export const validateCuit = (cuit: string): string | null => {
  if (!cuit) return 'El CUIT es requerido'
  if (!/^\d{11}$/.test(cuit.trim())) {
    return 'El CUIT debe contener exactamente 11 dígitos'
  }
  return null
}

// Nombre/Apellido: solo letras, espacios y guiones
export const validateName = (name: string, fieldName: string = 'El campo'): string | null => {
  if (!name) return `${fieldName} es requerido`
  
  const trimmed = name.trim()
  if (trimmed.length < 2) {
    return `${fieldName} debe tener al menos 2 caracteres`
  }
  
  if (!/^[a-záéíóúñàèìòùäëïöüA-ZÁÉÍÓÚÑÀÈÌÒÙÄËÏÖÜ\s\-]+$/.test(trimmed)) {
    return `${fieldName} solo puede contener letras, espacios y guiones (sin números)`
  }
  
  return null
}

// Número de afiliado: alfanumérico con guiones
export const validateNumAfiliado = (numAfiliado: string): string | null => {
  if (!numAfiliado) return 'El número de afiliado es requerido'
  
  if (!/^[a-zA-Z0-9\-]+$/.test(numAfiliado.trim())) {
    return 'El número de afiliado solo puede contener números, letras y guiones'
  }
  
  if (numAfiliado.trim().length < 1 || numAfiliado.trim().length > 50) {
    return 'El número de afiliado debe tener entre 1 y 50 caracteres'
  }
  
  return null
}

// Domicilio
export const validateDomicilio = (domicilio: string): string | null => {
  if (!domicilio) return 'El domicilio es requerido'
  
  const trimmed = domicilio.trim()
  if (trimmed.length < 5) {
    return 'El domicilio debe tener al menos 5 caracteres'
  }
  
  if (trimmed.length > 200) {
    return 'El domicilio no debe exceder 200 caracteres'
  }
  
  return null
}

// Email (opcional, pero si se proporciona debe ser válido)
export const validateEmail = (email: string | null | undefined): string | null => {
  if (!email || email.trim() === '') return null
  
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
    return 'El correo electrónico debe ser válido'
  }
  
  if (email.trim().length > 150) {
    return 'El correo no debe exceder 150 caracteres'
  }
  
  return null
}

// Validar paciente completo
export type PacienteErrors = {
  dni?: string
  nombre?: string
  apellido?: string
  numAfiliado?: string
  domicilio?: string
}

export const validatePaciente = (data: {
  dni: string
  nombre: string
  apellido: string
  numAfiliado: string
  domicilio: string
}): PacienteErrors => {
  const errors: PacienteErrors = {}

  const dniError = validateDni(data.dni)
  if (dniError) errors.dni = dniError

  const nombreError = validateName(data.nombre, 'El nombre')
  if (nombreError) errors.nombre = nombreError

  const apellidoError = validateName(data.apellido, 'El apellido')
  if (apellidoError) errors.apellido = apellidoError

  const numAfiliadoError = validateNumAfiliado(data.numAfiliado)
  if (numAfiliadoError) errors.numAfiliado = numAfiliadoError

  const domicilioError = validateDomicilio(data.domicilio)
  if (domicilioError) errors.domicilio = domicilioError

  return errors
}

// Validar obra social completa
export type ObraErrors = {
  cuit?: string
  nombre?: string
  domicilioComercial?: string
  mail?: string
}

export const validateObra = (data: {
  cuit: string
  nombre: string
  domicilioComercial: string
  mail: string | null
}): ObraErrors => {
  const errors: ObraErrors = {}

  const cuitError = validateCuit(data.cuit)
  if (cuitError) errors.cuit = cuitError

  const nombreError = validateName(data.nombre, 'El nombre')
  if (nombreError) errors.nombre = nombreError

  const domicilioError = validateDomicilio(data.domicilioComercial)
  if (domicilioError) errors.domicilioComercial = domicilioError

  const mailError = validateEmail(data.mail)
  if (mailError) errors.mail = mailError

  return errors
}

// Verificar si hay errores
export const hasErrors = (errors: Record<string, string | undefined>): boolean => {
  return Object.values(errors).some(error => error !== undefined && error !== null)
}
