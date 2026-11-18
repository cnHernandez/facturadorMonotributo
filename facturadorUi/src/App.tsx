import { useEffect, useState, useMemo } from 'react'
import {
  Building2,
  HeartPulse,
  Pencil,
  Plus,
  ReceiptText,
  Trash2,
  Users,
  X,
  AlertCircle,
  CheckCircle2,
  Eye,
  EyeOff,
  LogIn,
  LogOut,
  KeyRound,
} from 'lucide-react'
import {
  validateDni,
  validateCuit,
  validateName,
  validateNumAfiliado,
  validateDomicilio,
  validateEmail,
  hasErrors,
  type PacienteErrors,
  type ObraErrors,
} from './validations'

type Paciente = {
  id: number
  dni: string
  numAfiliado: string
  nombre: string
  apellido: string
  domicilio: string
  responsableVinculo?: string | null
  responsableNombre?: string | null
  responsableApellido?: string | null
  responsableDni?: string | null
  responsableDomicilio?: string | null
  responsableCondicionIVA?: CondicionIVA | null
  estado: boolean
  observaciones: string
  obraSocialId?: number | null
}

type CondicionIVA =
  | 'IVA_Sujeto_Exento'
  | 'ResponsableInscripto'
  | 'Consumidor_Final'

const condicionesIVA: {
  value: CondicionIVA
  label: string
}[] = [
  { value: 'IVA_Sujeto_Exento', label: 'IVA Sujeto Exento' },
  { value: 'ResponsableInscripto', label: 'Responsable Inscripto' },
  { value: 'Consumidor_Final', label: 'Consumidor Final' },
]

function nombreCondicionIVA(
  condicion: CondicionIVA | null | undefined
) {
  return condicionesIVA.find(
    (item) => item.value === condicion
  )?.label ?? 'Sin cargar'
}

type Obra = {
  id: number
  cuit: string
  nombre: string
  domicilioComercial: string
  condicion: 'Contado' | 'CuentaCorriente'
  mail: string | null
  condicionIVA: CondicionIVA | null
  condicionIVAConfirmada: boolean
  estado: boolean
}

type View = 'pacientes' | 'obras' | 'facturar'

const patientBlank = (): Omit<Paciente, 'id'> => ({
  dni: '',
  numAfiliado: '',
  nombre: '',
  apellido: '',
  domicilio: '',
  responsableVinculo: '',
  responsableNombre: '',
  responsableApellido: '',
  responsableDni: '',
  responsableDomicilio: '',
  responsableCondicionIVA: null,
  estado: true,
  obraSocialId: null,
  observaciones: '',
})

const obraBlank = (): Omit<Obra, 'id'> => ({
  cuit: '',
  nombre: '',
  domicilioComercial: '',
  mail: '',
  condicionIVA: null,
  condicionIVAConfirmada: false,
  condicion: 'Contado',
  estado: true,
})

async function api(
  path: string,
  method: string,
  body?: unknown
) {
  const response = await fetch(path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization:
        `Bearer ${window.sessionStorage.getItem(
          'lau_admin_token'
        ) ?? ''}`,
    },
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const responseBody = await response.text()
    throw new Error(
      responseBody
        ? `HTTP ${response.status}: ${responseBody}`
        : `HTTP ${response.status}`
    )
  }

  return response.status === 204
    ? null
    : response.json()
}

function ultimoDiaMesAnterior() {
  const hoy = new Date()

  const year = hoy.getFullYear()
  const month = String(hoy.getMonth() + 1).padStart(2, '0')
  const day = String(hoy.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}

function isOsdeObra(obra?: Obra | null) {
  return (
    obra?.nombre
      ?.toUpperCase()
      .includes('OSDE') ?? false
  )
}

function normalizeOptionalText(
  value: string | null | undefined
) {
  const normalized = value?.trim() ?? ''

  return normalized ? normalized : null
}

function hasResponsibleInvoiceData(
  paciente: Paciente | null
) {
  return Boolean(
    paciente?.responsableNombre?.trim() &&
    paciente.responsableApellido?.trim() &&
    paciente.responsableDni?.trim() &&
    paciente.responsableDomicilio?.trim() &&
    paciente.responsableCondicionIVA
  )
}

function hasAnyResponsibleData(
  paciente: Paciente | null
) {
  return Boolean(
    paciente?.responsableVinculo?.trim() ||
    paciente?.responsableNombre?.trim() ||
    paciente?.responsableApellido?.trim() ||
    paciente?.responsableDni?.trim() ||
    paciente?.responsableDomicilio?.trim() ||
    paciente?.responsableCondicionIVA
  )
}

function buildDefaultInvoiceDescription({
  paciente,
  fechaDesde,
}: {
  paciente: Paciente | null
  fechaDesde: string
}) {
  const fechaBase = fechaDesde
    ? new Date(`${fechaDesde}T00:00:00`)
    : new Date()

  const fechaValida = Number.isNaN(
    fechaBase.getTime()
  )
    ? new Date()
    : fechaBase

  const mesAnio = fechaValida
    .toLocaleDateString('es-AR', {
      month: 'long',
      year: 'numeric',
    })
    .toUpperCase()

  const descripcionBase =
    'HONORARIOS PROFESIONALES POR SESIONES DE PSICOPEDAGOGIA CORRESPONDIENTE AL MES ' +
    mesAnio

  const afiliado = paciente?.numAfiliado
    ? ` AF: ${paciente.numAfiliado}`
    : ''

  if (paciente) {
    const dni = paciente.dni
      ? ` DNI: ${paciente.dni.replace(/^(\d{2})(\d{3})(\d{3})$/, '$1.$2.$3')}`
      : ''

    return `${descripcionBase} PACIENTE: ${paciente.nombre} ${paciente.apellido}${dni}${afiliado}`
  }

  return `${descripcionBase}${afiliado}`
}

function buildFinalInvoiceDescription({
  descripcion,
  paciente,
}: {
  descripcion: string
  paciente: Paciente | null
}) {
  const descripcionBase =
    normalizeOptionalText(descripcion)

  if (!paciente) {
    return descripcionBase
  }

  const dni = paciente.dni
    ? ` DNI: ${paciente.dni.replace(/^(\d{2})(\d{3})(\d{3})$/, '$1.$2.$3')}`
    : ''

  const afiliado = paciente.numAfiliado
    ? ` AF: ${paciente.numAfiliado}`
    : ''

  const datosPaciente =
    ` PACIENTE: ${paciente.nombre} ${paciente.apellido}${dni}${afiliado}`

  if (!descripcionBase) {
    return datosPaciente.trim()
  }

  if (/\bPACIENTE\s*:/.test(descripcionBase)) {
    return descripcionBase
  }

  return `${descripcionBase}${datosPaciente}`
}

function App() {
  const maxFechaFacturacion = useMemo(
    () => ultimoDiaMesAnterior(),
    [] // No dependencies - recalcula en cada render
  )
  const [view, setView] = useState<View>('pacientes')
  const [message, setMessage] = useState('Cargando datos...')
  const [isAuthenticated, setIsAuthenticated] =
    useState(
      () =>
        Boolean(
          window.sessionStorage.getItem(
            'lau_admin_token'
          )
        )
    )
  const [loginUsername, setLoginUsername] =
    useState('')
  const [loginPassword, setLoginPassword] =
    useState('')
  const [showLoginPassword, setShowLoginPassword] =
    useState(false)
  const [loginError, setLoginError] =
    useState('')
  const [showPasswordModal, setShowPasswordModal] =
    useState(false)
  const [currentPassword, setCurrentPassword] =
    useState('')
  const [newPassword, setNewPassword] =
    useState('')
  const [confirmPassword, setConfirmPassword] =
    useState('')
  const [showChangePassword, setShowChangePassword] =
    useState(false)
  const [passwordMessage, setPasswordMessage] =
    useState('')

  const [pacientes, setPacientes] = useState<Paciente[]>([])
  const [obras, setObras] = useState<Obra[]>([])

  const [patient, setPatient] =
    useState<Omit<Paciente, 'id'>>(patientBlank())

  const [obra, setObra] =
    useState<Omit<Obra, 'id'>>(obraBlank())

  const [patientId, setPatientId] =
    useState<number | null>(null)

  const [obraId, setObraId] =
    useState<number | null>(null)

  const [showPatientForm, setShowPatientForm] =
    useState(false)

  const [showObraForm, setShowObraForm] =
    useState(false)

  const [targetKind, setTargetKind] =
    useState<'paciente' | 'obra'>('paciente')

  const [targetId, setTargetId] =
    useState('')

  const [invoicePatientId, setInvoicePatientId] =
    useState('')

  const [amount, setAmount] =
    useState('1000')

  const [cantidadSesiones, setCantidadSesiones] =
    useState('1')

  const [precioSesion, setPrecioSesion] =
    useState('1000')

  const [fechaDesde, setFechaDesde] =
    useState('')

  const [fechaHasta, setFechaHasta] =
    useState('')

  const [fechaHastaError, setFechaHastaError] =
    useState('')

  const [fechaVencimiento, setFechaVencimiento] =
    useState('')

  const [fechaVencimientoError, setFechaVencimientoError] =
    useState('')

  const [invoiceDescription, setInvoiceDescription] =
    useState('')

  useEffect(() => {
    if (!isAuthenticated) {
      return
    }

    Promise.all([
      api('/api/pacientes', 'GET'),
      api('/api/obras-sociales', 'GET'),
    ])
      .then(([patientData, obraData]) => {
        const loadedPatients =
  ((patientData || []) as Paciente[])
    .filter((x) => x.estado)

const loadedObras =
  ((obraData || []) as Obra[])
    .filter((x) => x.estado)

        setPacientes(loadedPatients)
        setObras(loadedObras)

        const loadedParticulares = loadedPatients.filter(
          (paciente) => paciente.obraSocialId == null
        )

        if (loadedParticulares.length > 0) {
          setTargetKind('paciente')
          setTargetId(
            String(loadedParticulares[0].id)
          )
        } else if (loadedObras.length > 0) {
          const firstObra = loadedObras[0]
          const firstObraEsOsde =
            isOsdeObra(firstObra)

          setTargetKind('obra')
          setTargetId(
            String(firstObra.id)
          )

          const firstPatients =
            loadedPatients.filter(
              (paciente) =>
                paciente.obraSocialId ===
                firstObra.id
            )

          setInvoicePatientId(
            !firstObraEsOsde &&
              firstPatients.length > 0
              ? String(firstPatients[0].id)
              : ''
          )
        }

        setMessage(
          loadedPatients.length > 0 ||
            loadedObras.length > 0
            ? 'Datos cargados correctamente'
            : 'Base de datos vacía'
        )
      })
      .catch((error) => {
        console.error(error)

        setMessage(
          'Error conectando con API'
        )
      })
  }, [isAuthenticated])

  const openNewPatient = () => {
    setPatient(patientBlank())
    setPatientId(null)
    setShowPatientForm(true)
  }

  const openEditPatient = (item: Paciente) => {
    const { id, ...draft } = item

    setPatientId(id)
    setPatient(draft)
    setShowPatientForm(true)
  }

  const closePatientForm = () => {
    setShowPatientForm(false)
    setPatient(patientBlank())
    setPatientId(null)
  }

  const openNewObra = () => {
    setObra(obraBlank())
    setObraId(null)
    setShowObraForm(true)
  }

  const openEditObra = (item: Obra) => {
    const { id, ...draft } = item

    setObraId(id)
    setObra(draft)
    setShowObraForm(true)
  }

  const closeObraForm = () => {
    setShowObraForm(false)
    setObra(obraBlank())
    setObraId(null)
  }

  const savePatient = async (
    event: React.FormEvent
  ) => {
    event.preventDefault()

    try {
      const patientPayload = {
        ...patient,
        responsableVinculo: normalizeOptionalText(
          patient.responsableVinculo
        ),
        responsableNombre: normalizeOptionalText(
          patient.responsableNombre
        ),
        responsableApellido: normalizeOptionalText(
          patient.responsableApellido
        ),
        responsableDni: normalizeOptionalText(
          patient.responsableDni
        ),
        responsableDomicilio: normalizeOptionalText(
          patient.responsableDomicilio
        ),
      }

      const data = await api(
        patientId
          ? `/api/pacientes/${patientId}`
          : '/api/pacientes',
        patientId ? 'PUT' : 'POST',
        patientPayload
      )

      const savedPatient =
        (data ?? {
          ...patientPayload,
          id: patientId ?? Date.now(),
        }) as Paciente

      setPacientes((items) =>
        patientId
          ? items.map((item) =>
              item.id === patientId
                ? savedPatient
                : item
            )
          : [...items, savedPatient]
      )

      setMessage(
        patientId
          ? 'Paciente actualizado correctamente'
          : 'Paciente creado correctamente'
      )

      closePatientForm()
    } catch (error) {
      console.error(error)

      setMessage(
        'No se pudo guardar el paciente'
      )
    }
  }

  const handleLogin = async (
    event: React.FormEvent
  ) => {
    event.preventDefault()

    try {
      const response =
        await api('/api/auth/login', 'POST', {
          userName: loginUsername.trim(),
          password: loginPassword,
        }) as {
          token: string
          userName: string
        }

      window.sessionStorage.setItem(
        'lau_admin_token',
        response.token
      )
      setLoginError('')
      setIsAuthenticated(true)
    } catch {
      setLoginError(
        'Usuario o contraseña incorrectos'
      )
    }
  }

  const handleLogout = () => {
    window.sessionStorage.removeItem(
      'lau_admin_token'
    )
    setLoginPassword('')
    setLoginError('')
    setIsAuthenticated(false)
  }

  const openPasswordModal = () => {
    setCurrentPassword('')
    setNewPassword('')
    setConfirmPassword('')
    setPasswordMessage('')
    setShowChangePassword(false)
    setShowPasswordModal(true)
  }

  const closePasswordModal = () => {
    setShowPasswordModal(false)
    setPasswordMessage('')
  }

  const handleChangePassword = async (
    event: React.FormEvent
  ) => {
    event.preventDefault()
    setPasswordMessage('')

    if (newPassword.length < 8) {
      setPasswordMessage(
        'La nueva contraseña debe tener al menos 8 caracteres'
      )
      return
    }

    if (newPassword !== confirmPassword) {
      setPasswordMessage(
        'Las contraseñas nuevas no coinciden'
      )
      return
    }

    if (newPassword === currentPassword) {
      setPasswordMessage(
        'La nueva contraseña debe ser distinta'
      )
      return
    }

    try {
      await api(
        '/api/auth/change-password',
        'POST',
        {
          currentPassword,
          newPassword,
        }
      )

      setPasswordMessage(
        'Contraseña cambiada correctamente'
      )
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (error) {
      console.error(error)
      setPasswordMessage(
        'No se pudo cambiar la contraseña. Revisá la contraseña actual.'
      )
    }
  }

  const saveObra = async (
    event: React.FormEvent
  ) => {
    event.preventDefault()

    try {
      const obraPayload = {
        ...obra,
        mail: normalizeOptionalText(
          obra.mail
        ),
      }

      const data = await api(
        obraId
          ? `/api/obras-sociales/${obraId}`
          : '/api/obras-sociales',
        obraId ? 'PUT' : 'POST',
        obraPayload
      )

      const savedObra =
        (data ?? {
          ...obraPayload,
          id: obraId ?? Date.now(),
        }) as Obra

      setObras((items) =>
        obraId
          ? items.map((item) =>
              item.id === obraId
                ? savedObra
                : item
            )
          : [...items, savedObra]
      )

      setMessage(
        obraId
          ? 'Obra social actualizada correctamente'
          : 'Obra social creada correctamente'
      )

      closeObraForm()
    } catch (error) {
      console.error(error)

      setMessage(
        'No se pudo guardar la obra social'
      )
    }
  }

  const remove = async (
    kind: 'pacientes' | 'obras-sociales',
    id: number
  ) => {
    if (!confirm('¿Eliminar este registro?')) {
      return
    }

    try {
      await api(
        `/api/${kind}/${id}`,
        'DELETE'
      )

      if (kind === 'pacientes') {
        setPacientes((items) =>
          items.filter(
            (item) => item.id !== id
          )
        )

        if (
          targetKind === 'paciente' &&
          targetId === String(id)
        ) {
          const remaining =
            pacientes.filter(
              (item) => item.id !== id
            )

          setTargetId(
            remaining.length > 0
              ? String(remaining[0].id)
              : ''
          )
        }

        if (
          invoicePatientId === String(id)
        ) {
          setInvoicePatientId('')
        }
      } else {
        setObras((items) =>
          items.filter(
            (item) => item.id !== id
          )
        )

        if (
          targetKind === 'obra' &&
          targetId === String(id)
        ) {
          const remaining =
            obras.filter(
              (item) => item.id !== id
            )

          if (remaining.length > 0) {
            setTargetId(
              String(remaining[0].id)
            )

            const associatedPatients =
              pacientes.filter(
                (patient) =>
                  patient.obraSocialId ===
                  remaining[0].id
              )

            const remainingEsOsde =
              isOsdeObra(remaining[0])

            setInvoicePatientId(
              !remainingEsOsde &&
                associatedPatients.length > 0
                ? String(
                    associatedPatients[0].id
                  )
                : ''
            )
          } else {
            setTargetId('')
            setInvoicePatientId('')
          }
        }
      }

      setMessage(
        'Registro eliminado correctamente'
      )
    } catch (error) {
      console.error(error)

      setMessage(
        'No se pudo eliminar el registro'
      )
    }
  }

  const particulares = pacientes.filter(
    (paciente) => paciente.obraSocialId == null
  )

  const invoicePatients =
    targetKind === 'obra'
      ? pacientes.filter(
          (paciente) =>
            paciente.obraSocialId ===
            Number(targetId)
        )
      : particulares

  const selectedObra =
    targetKind === 'obra'
      ? obras.find(
          (obra) =>
            obra.id === Number(targetId)
        ) ?? null
      : null

  const esOsde = isOsdeObra(selectedObra)

  const selectedPatient =
    targetKind === 'paciente'
      ? particulares.find(
          (paciente) =>
            paciente.id === Number(targetId)
        ) ?? null
      : pacientes.find(
          (x) =>
            x.id ===
            Number(invoicePatientId)
        ) ?? null

  const invoiceToResponsible =
    targetKind === 'paciente' &&
    selectedPatient?.obraSocialId == null &&
    hasResponsibleInvoiceData(selectedPatient)

  const condicionIVAReceptor =
    targetKind === 'obra'
      ? selectedObra?.condicionIVA
      : invoiceToResponsible
        ? selectedPatient?.responsableCondicionIVA
        : selectedPatient?.obraSocialId != null
          ? selectedPatient?.responsableCondicionIVA
          : null

  const suggestedInvoiceDescription =
    buildDefaultInvoiceDescription({
      paciente: selectedPatient,
      fechaDesde,
    })

  const finalInvoiceDescription =
    buildFinalInvoiceDescription({
      descripcion:
        normalizeOptionalText(invoiceDescription) ??
        suggestedInvoiceDescription,
      paciente: selectedPatient,
    })

  const downloadPdf = async () => {
    if (
      targetKind === 'paciente' &&
      !selectedPatient
    ) {
      setMessage(
        'Seleccioná un paciente para facturar'
      )
      return
    }

    if (
      targetKind === 'paciente' &&
      selectedPatient &&
      selectedPatient.obraSocialId == null &&
      !hasResponsibleInvoiceData(selectedPatient)
    ) {
      setMessage(
        'Para facturar particular, cargá en la ficha nombre, apellido, DNI, domicilio y condición frente al IVA del tutor'
      )
      return
    }

    if (
      targetKind === 'paciente' &&
      selectedPatient &&
      selectedPatient.obraSocialId == null
    ) {
      if (!hasResponsibleInvoiceData(selectedPatient)) {
        setMessage(
          'Completá nombre, apellido, DNI, domicilio y condición frente al IVA del padre/madre o tutor en la ficha del paciente'
        )
        return
      }

      const dniResponsable = validateDni(
        selectedPatient.responsableDni ?? ''
      )

      if (dniResponsable) {
        setMessage(
          'El DNI del padre/madre o tutor debe contener exactamente 8 dígitos'
        )
        return
      }

      const nombreResponsable =
        `${selectedPatient.responsableNombre ?? ''} ${selectedPatient.responsableApellido ?? ''}`.trim()

      if (
        validateName(nombreResponsable, 'El nombre del responsable') ||
        nombreResponsable.length > 150
      ) {
        setMessage(
          'Revisá el nombre y apellido del padre/madre o tutor'
        )
        return
      }

      if (
        validateDomicilio(
          selectedPatient.responsableDomicilio ?? ''
        )
      ) {
        setMessage(
          'El domicilio del padre/madre o tutor debe tener entre 5 y 200 caracteres'
        )
        return
      }
    }

    if (
      targetKind === 'obra' &&
      (!selectedObra ||
        (!esOsde && !selectedPatient))
    ) {
      setMessage(
        'Seleccioná la obra social y el paciente'
      )
      return
    }

    if (
      targetKind === 'obra' &&
      selectedObra &&
      !selectedObra.condicionIVAConfirmada
    ) {
      setMessage(
        'Revisá y confirmá la condición frente al IVA de la obra social antes de emitir la factura'
      )
      return
    }

    if (!condicionIVAReceptor) {
      setMessage(
        'Cargá la condición frente al IVA correcta del receptor en su ficha antes de emitir la factura'
      )
      return
    }

    // Validar fechas
    
if (!fechaDesde || !fechaHasta) {
  setMessage(
    'Ingresá el período facturado (desde - hasta)'
  )
  return
}

if (fechaHastaError) {
  setMessage(
    'Corregí el error en las fechas: ' + fechaHastaError
  )
  return
}

if (
  fechaDesde > maxFechaFacturacion ||
  fechaHasta > maxFechaFacturacion
) {
  setMessage(
    'El período facturado no puede ser en el futuro'
  )
  return
}

if (fechaDesde > fechaHasta) {
  setMessage(
    'La fecha desde no puede ser posterior a la fecha hasta'
  )
  return
}

if (!fechaVencimiento) {
  setMessage(
    'Ingresá la fecha de vencimiento'
  )
  return
}

if (fechaVencimientoError) {
  setMessage(
    'Corregí el error en la fecha de vencimiento: ' + fechaVencimientoError
  )
  return
}

    // Calcular importe total
    let numericAmount = 0
    
    if (esOsde) {
      // Para OSDE, usar el importe manual
      numericAmount = Number(
        amount.replace(',', '.')
      )
    } else {
      // Para otros casos, calcular cantidad × precio
      const cantidad = Number(
        cantidadSesiones.replace(',', '.') || 1
      )
      const precio = Number(
        precioSesion.replace(',', '.') || 0
      )
      numericAmount = cantidad * precio
    }

    if (
      !Number.isFinite(numericAmount) ||
      numericAmount <= 0
    ) {
      setMessage(
        'Ingresá un importe válido'
      )
      return
    }

    if (esOsde && !amount) {
      setMessage(
        'Ingresá un importe válido para OSDE'
      )
      return
    }

    if (!esOsde && (!cantidadSesiones || !precioSesion)) {
      setMessage(
        'Ingresá cantidad de sesiones y precio'
      )
      return
    }

    try {
      setMessage(
        'Emitiendo factura...'
      )

      const response = await fetch(
        '/api/arca/cae/pdf',
        {
          method: 'POST',
          headers: {
            'Content-Type':
              'application/json',
          },
          body: JSON.stringify({
            tipoComprobante: 11,

            tipoDocumentoReceptor:
              targetKind === 'paciente'
                ? 96
                : 80,

            numeroDocumentoReceptor:
              targetKind === 'paciente'
                ? Number(
                    (invoiceToResponsible
                      ? selectedPatient!.responsableDni!
                      : selectedPatient!.dni
                    ).replaceAll('-', '')
                  )
                : Number(
                    selectedObra!.cuit.replaceAll('-', '')
                  ),

            condicionIVAReceptor,

            nombreReceptor:
              targetKind === 'paciente'
                ? invoiceToResponsible
                  ? `${selectedPatient!.responsableNombre ?? ''} ${selectedPatient!.responsableApellido ?? ''}`
                  : `${selectedPatient!.nombre} ${selectedPatient!.apellido}`
                : selectedObra!.nombre,

            domicilioReceptor:
              targetKind === 'paciente'
                ? invoiceToResponsible
                  ? selectedPatient!.responsableDomicilio
                  : selectedPatient!.domicilio
                : selectedObra!.domicilioComercial,

            condicionVenta:
              targetKind === 'obra'
                ? selectedObra!.condicion
                : 'Contado',

            importeTotal: numericAmount,

            cantidadSesiones: esOsde ? undefined : Number(cantidadSesiones),
            precioSesion: esOsde ? undefined : Number(precioSesion),

            fechaDesde,
            fechaHasta,
            fechaVencimiento,

            esFacturacionManual: esOsde,

            pacienteNombre:
              selectedPatient
                ? `${selectedPatient.nombre} ${selectedPatient.apellido}`
                : null,

            pacienteDni:
              selectedPatient
                ? selectedPatient.dni
                : null,

            pacienteNumAfiliado:
              selectedPatient
                ? selectedPatient.numAfiliado
                : null,

            descripcionServicio:
              finalInvoiceDescription,
          }),
        }
      )

      if (!response.ok) {
        let errorMessage =
          `HTTP ${response.status}`

        try {
          const errorBody =
            await response.text()

          if (errorBody) {
            console.error(
              'Respuesta del servidor:',
              errorBody
            )
          }
        } catch {
          // Ignorar error de lectura
        }

        throw new Error(errorMessage)
      }

      const blob =
        await response.blob()

      const url =
        URL.createObjectURL(blob)

      const link =
        document.createElement('a')

      link.href = url
      
      // Obtener nombre del archivo del header Content-Disposition
      const contentDisposition = response.headers.get('content-disposition')
      let fileName = 'factura.pdf'
      
      if (contentDisposition) {
        // Intentar extraer filename de: attachment; filename="FacturaC-00000001.pdf"
        const filenameRegex = /filename[^;=\n]*=(?:UTF-8'')?(?:"([^"]*)"|'([^']*)'|([^;\n]*))/i
        const matches = contentDisposition.match(filenameRegex)
        if (matches && (matches[1] || matches[2] || matches[3])) {
          fileName = (matches[1] || matches[2] || matches[3]).trim()
        }
      }
      
      link.download = fileName

      document.body.appendChild(link)

      link.click()

      link.remove()

      URL.revokeObjectURL(url)

      setMessage(
        'Factura emitida y descargada correctamente'
      )
    } catch (error) {
      console.error(error)

      setMessage(
        'No se pudo emitir la factura'
      )
    }
  }

  const nav = [
    {
      id: 'pacientes' as const,
      label: 'Pacientes',
      icon: Users,
    },
    {
      id: 'obras' as const,
      label: 'Obras sociales',
      icon: Building2,
    },
    {
      id: 'facturar' as const,
      label: 'Facturar',
      icon: ReceiptText,
    },
  ]

  if (!isAuthenticated) {
    return (
      <main className="login-page">
        <section className="login-card" aria-label="Inicio de sesión">
          <div className="login-brand">
            <div className="login-brand-icon">
              <HeartPulse size={26} />
            </div>
            <div>
              <p>Lau · Gestión clínica</p>
              <span>Sistema de facturación</span>
            </div>
          </div>

          <div className="login-heading">
            <h1>Bienvenida</h1>
            <p>Ingresá tus datos para continuar</p>
          </div>

          <form onSubmit={handleLogin} className="login-form">
            <Field label="Usuario">
              <input
                autoComplete="username"
                value={loginUsername}
                onChange={(event) =>
                  setLoginUsername(
                    event.target.value
                  )
                }
                placeholder="Ingresá tu usuario"
              />
            </Field>

            <Field label="Contraseña" error={loginError}>
              <div className="login-password-wrap">
                <input
                  type={
                    showLoginPassword
                      ? 'text'
                      : 'password'
                  }
                  autoComplete="current-password"
                  value={loginPassword}
                  onChange={(event) =>
                    setLoginPassword(
                      event.target.value
                    )
                  }
                  placeholder="Ingresá tu contraseña"
                />
                <button
                  type="button"
                  className="login-password-toggle"
                  onClick={() =>
                    setShowLoginPassword(
                      (visible) => !visible
                    )
                  }
                  aria-label={
                    showLoginPassword
                      ? 'Ocultar contraseña'
                      : 'Mostrar contraseña'
                  }
                  title={
                    showLoginPassword
                      ? 'Ocultar contraseña'
                      : 'Mostrar contraseña'
                  }
                >
                  {showLoginPassword
                    ? <EyeOff size={18} />
                    : <Eye size={18} />}
                </button>
              </div>
            </Field>

            <button
              type="submit"
              className="login-submit"
            >
              <LogIn size={18} />
              Iniciar sesión
            </button>
          </form>

          <p className="login-footer">
            Acceso administrativo
          </p>
        </section>
      </main>
    )
  }

  return (
    <div className="min-h-screen bg-[#edf3f0] text-slate-800">
      <header className="flex h-16 items-center justify-between border-b border-emerald-950/10 bg-white px-5 md:px-8">
        <div className="flex items-center gap-3">
          <div className="grid size-8 place-items-center rounded-md bg-emerald-700 text-white">
            <HeartPulse size={18} />
          </div>

          <div>
            <p className="text-sm font-semibold text-emerald-950">
              Lau · Gestión clínica
            </p>

            <p className="text-xs text-slate-500">
              Facturación y pacientes
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <span className="hidden text-xs text-slate-500 sm:block">
            {message}
          </span>
          <button
            type="button"
            onClick={openPasswordModal}
            className="icon-button"
            aria-label="Cambiar contraseña"
            title="Cambiar contraseña"
          >
            <KeyRound size={18} />
          </button>
          <button
            type="button"
            onClick={handleLogout}
            className="icon-button"
            aria-label="Cerrar sesión"
            title="Cerrar sesión"
          >
            <LogOut size={18} />
          </button>
        </div>
      </header>

      <div className="mx-auto grid max-w-[1440px] md:grid-cols-[214px_1fr]">
        <aside className="border-b border-emerald-950/10 bg-[#e4eee8] p-3 md:min-h-[calc(100vh-4rem)] md:border-b-0 md:border-r">
          <p className="px-3 pb-3 pt-2 text-[11px] font-bold uppercase text-emerald-900/55">
            Operación
          </p>

          <nav className="flex gap-1 overflow-x-auto md:flex-col">
            {nav.map((item) => (
              <button
                key={item.id}
                type="button"
                onClick={() =>
                  setView(item.id)
                }
                className={`flex shrink-0 items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium ${
                  view === item.id
                    ? 'bg-emerald-800 text-white'
                    : 'text-emerald-950/70 hover:bg-white/70'
                }`}
              >
                <item.icon size={18} />
                {item.label}
              </button>
            ))}
          </nav>

          <div className="mt-8 hidden border-t border-emerald-950/10 px-3 pt-5 text-xs text-emerald-950/55 md:block">
            <b>Homologación ARCA</b>

            <p className="mt-1">
              Punto de venta 0004
            </p>
          </div>
        </aside>

        <main className="p-4 md:p-8">
          {/* PACIENTES */}
          {view === 'pacientes' && (
            <section className="space-y-6">
              <Title
                title="Pacientes"
                text="Registro y seguimiento de afiliaciones"
                button={
                  <button
                    type="button"
                    onClick={openNewPatient}
                    className="command"
                  >
                    <Plus size={17} />
                    Nuevo paciente
                  </button>
                }
              />

              <PatientsTable
                items={pacientes}
                obras={obras}
                edit={openEditPatient}
                remove={(id) =>
                  remove(
                    'pacientes',
                    id
                  )
                }
              />
            </section>
          )}

          {/* OBRAS SOCIALES */}
          {view === 'obras' && (
            <section className="space-y-6">
              <Title
                title="Obras sociales"
                text="Convenios y condiciones comerciales"
                button={
                  <button
                    type="button"
                    onClick={openNewObra}
                    className="command"
                  >
                    <Plus size={17} />
                    Nueva obra social
                  </button>
                }
              />

              <ObrasTable
                items={obras}
                edit={openEditObra}
                remove={(id) =>
                  remove(
                    'obras-sociales',
                    id
                  )
                }
              />
            </section>
          )}

          {/* FACTURAR */}
          {view === 'facturar' && (
            <section className="space-y-6">
              <Title
                title="Facturar"
                text="Emisión de comprobantes electrónicos"
              />

              <div className="w-full">
                <div className="panel p-6">

                  <div className="mb-6 flex gap-2 border-b border-slate-200">
                    <button
                      type="button"
                      onClick={() => {
                        setTargetKind(
                          'paciente'
                        )

                        setTargetId(
                          String(
                            particulares[0]?.id ??
                              ''
                          )
                        )

                        setInvoicePatientId(
                          ''
                        )
                        setInvoiceDescription(
                          ''
                        )
                      }}
                      className={`tab ${
                        targetKind ===
                        'paciente'
                          ? 'tab-active'
                          : ''
                      }`}
                    >
                      <Users size={16} />
                      Paciente
                    </button>

                    <button
                      type="button"
                      onClick={() => {
                        setTargetKind(
                          'obra'
                        )

                        const firstObra =
                          obras[0]

                        if (!firstObra) {
                          setTargetId('')
                          setInvoicePatientId(
                            ''
                          )
                          setInvoiceDescription(
                            ''
                          )

                          return
                        }

                        const firstObraEsOsde =
                          isOsdeObra(firstObra)

                        setTargetId(
                          String(
                            firstObra.id
                          )
                        )

                        const associatedPatients =
                          pacientes.filter(
                            (paciente) =>
                              paciente.obraSocialId ===
                              firstObra.id
                          )

                        setInvoicePatientId(
                          !firstObraEsOsde &&
                          associatedPatients.length >
                          0
                            ? String(
                                associatedPatients[0]
                                  .id
                              )
                            : ''
                        )
                        setInvoiceDescription(
                          ''
                        )
                      }}
                      className={`tab ${
                        targetKind === 'obra'
                          ? 'tab-active'
                          : ''
                      }`}
                    >
                      <Building2 size={16} />
                      Obra social
                    </button>
                  </div>

                  {/* FACTURA A PACIENTE */}
                  {targetKind ===
                    'paciente' && (
                    <div className="space-y-5">
                      <div className="grid gap-5 md:grid-cols-2">
                        <Field label="Paciente">
                          <select
                            value={
                              invoicePatients.some((p) => p.id === Number(targetId))
                                ? targetId
                                : ''
                            }
                            onChange={(event) => {
                              setTargetId(
                                event.target.value
                              )
                            }}
                          >
                            {invoicePatients.length ===
                            0 ? (
                              <option value="">
                                No hay pacientes particulares
                              </option>
                            ) : (
                              invoicePatients.map(
                                (paciente) => (
                                  <option
                                    key={
                                      paciente.id
                                    }
                                    value={
                                      paciente.id
                                    }
                                  >
                                    {
                                      paciente.apellido
                                    }
                                    ,{' '}
                                    {
                                      paciente.nombre
                                    }{' '}
                                    — DNI{' '}
                                    {
                                      paciente.dni
                                    }
                                  </option>
                                )
                              )
                            )}
                          </select>
                        </Field>

                        {selectedPatient &&
                          selectedPatient.obraSocialId == null &&
                          hasResponsibleInvoiceData(selectedPatient) && (
                            <p className="self-end pb-2 text-sm font-medium text-slate-700">
                              Receptor particular:{' '}
                              {selectedPatient.responsableVinculo?.toLowerCase() ||
                                'padre/madre o tutor'}
                              {' ('}
                              {selectedPatient.responsableNombre}{' '}
                              {selectedPatient.responsableApellido}
                              {')'}
                            </p>
                          )}

                        {selectedPatient?.obraSocialId == null &&
                          hasAnyResponsibleData(selectedPatient) &&
                          !hasResponsibleInvoiceData(selectedPatient) && (
                            <p className="text-sm text-amber-700 md:col-span-2">
                              Para facturar particular, completá en la ficha nombre, apellido, DNI, domicilio y condición frente al IVA del padre/madre o tutor.
                            </p>
                          )}

                        <Field label="Cantidad de sesiones">
  <input
    type="number"
    min="1"
    step="1"
    value={cantidadSesiones}
    onChange={(event) =>
      setCantidadSesiones(event.target.value)
    }
  />
</Field>
                      </div>

                      <div className="grid gap-5 md:grid-cols-2">
                        <Field label="Precio por sesión">
                          <input
                            value={precioSesion}
                            onChange={(event) =>
                              setPrecioSesion(
                                event.target.value
                              )
                            }
                            inputMode="decimal"
                            placeholder="1000"
                          />
                        </Field>

                        <div />
                      </div>

                      <div className="grid gap-5 md:grid-cols-3">
                       <Field label="Período desde">
  <input
    type="date"
    max={maxFechaFacturacion}
    value={fechaDesde}
    onChange={(event) => {
      setFechaDesde(event.target.value)
      // Validar que fechaHasta no sea menor que la nueva fechaDesde
      if (fechaHasta && event.target.value > fechaHasta) {
        setFechaHastaError(
          'La fecha hasta no puede ser menor a la fecha desde'
        )
      } else {
        setFechaHastaError('')
      }
    }}
  />
</Field>

<Field label="Período hasta">
  <input
    type="date"
    max={maxFechaFacturacion}
    value={fechaHasta}
    onChange={(event) => {
      const nuevaFechaHasta = event.target.value
      setFechaHasta(nuevaFechaHasta)
      // Validar que no sea menor que fechaDesde
      if (fechaDesde && nuevaFechaHasta < fechaDesde) {
        setFechaHastaError(
          'La fecha hasta no puede ser menor a la fecha desde'
        )
      } else {
        setFechaHastaError('')
      }
      // Validar que no sea mayor que fechaVencimiento
      if (fechaVencimiento && nuevaFechaHasta > fechaVencimiento) {
        setFechaVencimientoError(
          'La fecha de vencimiento no puede ser menor a la fecha hasta'
        )
      } else if (fechaVencimiento && nuevaFechaHasta <= fechaVencimiento) {
        setFechaVencimientoError('')
      }
    }}
  />
  {fechaHastaError && (
    <p className="mt-1 text-sm text-red-500 font-semibold">
      ⚠️ {fechaHastaError}
    </p>
  )}
</Field>

                        <Field label="Fecha de Vto.">
                          <input
                            type="date"
                            value={fechaVencimiento}
                            onChange={(event) => {
                              const nuevaFechaVencimiento = event.target.value
                              setFechaVencimiento(nuevaFechaVencimiento)
                              // Validar que no sea menor a fechaHasta
                              if (fechaHasta && nuevaFechaVencimiento < fechaHasta) {
                                setFechaVencimientoError(
                                  'La fecha de vencimiento no puede ser menor a la fecha hasta'
                                )
                              } else {
                                setFechaVencimientoError('')
                              }
                            }}
                          />
                          {fechaVencimientoError && (
                            <p className="mt-1 text-sm text-red-500 font-semibold">
                              ⚠️ {fechaVencimientoError}
                            </p>
                          )}
                        </Field>
                      </div>
                    </div>
                  )}

                  {/* FACTURA A OBRA SOCIAL */}
                  {targetKind === 'obra' && (
                    <div className="space-y-5">
                      <div className="grid gap-5 md:grid-cols-2">
                        <Field label="Obra social">
                          <select
                            value={
                              targetId
                            }
                            onChange={(
                              event
                            ) => {
                              const obraId =
                                event.target
                                  .value

                              setTargetId(
                                obraId
                              )

                              setInvoiceDescription(
                                ''
                              )

                              const patients =
                                pacientes.filter(
                                  (paciente) =>
                                    paciente.obraSocialId ===
                                    Number(
                                      obraId
                                    )
                                )

                              const obraSeleccionada =
                                obras.find(
                                  (obra) =>
                                    obra.id ===
                                    Number(
                                      obraId
                                    )
                                )

                              const obraEsOsde =
                                isOsdeObra(
                                  obraSeleccionada
                                )

                              setInvoicePatientId(
                                !obraEsOsde &&
                                patients.length >
                                0
                                  ? String(
                                      patients[0]
                                        .id
                                    )
                                  : ''
                              )
                            }}
                          >
                            {obras.length ===
                            0 ? (
                              <option value="">
                                No hay obras sociales
                              </option>
                            ) : (
                              obras.map(
                                (obra) => (
                                  <option
                                    key={
                                      obra.id
                                    }
                                    value={
                                      obra.id
                                    }
                                  >
                                    {
                                      obra.nombre
                                    }
                                  </option>
                                )
                              )
                            )}
                          </select>
                        </Field>

                        

                        <Field
                          label={
                            esOsde
                              ? 'Paciente (opcional para OSDE)'
                              : 'Paciente'
                          }
                        >
                          <select
                            value={
                              invoicePatientId
                            }
                            onChange={(
                              event
                            ) =>
                              setInvoicePatientId(
                                event.target
                                  .value
                              )
                            }
                            disabled={
                              !esOsde &&
                              invoicePatients.length ===
                                0
                            }
                          >
                            {esOsde && (
                              <option value="">
                                Facturación global OSDE
                              </option>
                            )}

                            {invoicePatients.length ===
                            0 ? (
                              <option value="">
                                No hay pacientes asociados
                                a esta obra social
                              </option>
                            ) : (
                              invoicePatients.map(
                                (paciente) => (
                                  <option
                                    key={
                                      paciente.id
                                    }
                                    value={
                                      paciente.id
                                    }
                                  >
                                    {
                                      paciente.apellido
                                    }
                                    ,{' '}
                                    {
                                      paciente.nombre
                                    }{' '}
                                    — DNI{' '}
                                    {
                                      paciente.dni
                                    }
                                  </option>
                                )
                              )
                            )}
                          </select>
                        </Field>
                      </div>
<div className={esOsde ? 'w-full md:w-[100%]' : 'w-full'}>
  <Field label="Producto / servicio">
    <textarea
      value={invoiceDescription}
      onChange={(event) =>
        setInvoiceDescription(event.target.value)
      }
      rows={3}
      className="w-full"
      placeholder={suggestedInvoiceDescription}
    />
  </Field>
</div>

                      {/* Si es OSDE, mostrar importe manual */}
                      {esOsde ? (
                          <div className="grid gap-5 md:grid-cols-2">
                            <Field label="Importe total">
                              <input
                                value={amount}
                                onChange={(event) =>
                                  setAmount(
                                    event.target.value
                                  )
                                }
                                inputMode="decimal"
                                placeholder="1000"
                              />
                            </Field>
                            <div />
                          </div>
                        ) : (
                          <>
                            <div className="grid gap-5 md:grid-cols-2">
                              <Field label="Cantidad de sesiones">
                               <input
  type="number"
  min="1"
  step="1"
  value={cantidadSesiones}
  onChange={(event) =>
    setCantidadSesiones(event.target.value)
  }
/>
                              </Field>

                              <Field label="Precio por sesión">
                                <input
                                  value={precioSesion}
                                  onChange={(event) =>
                                    setPrecioSesion(
                                      event.target.value
                                    )
                                  }
                                  inputMode="decimal"
                                  placeholder="1000"
                                />
                              </Field>
                            </div>
                          </>
                        )}

                      <div className="grid gap-5 md:grid-cols-3">
                       <Field label="Período desde">
  <input
    type="date"
    max={maxFechaFacturacion}
    value={fechaDesde}
    onChange={(event) => {
      setFechaDesde(event.target.value)
      // Validar que fechaHasta no sea menor que la nueva fechaDesde
      if (fechaHasta && event.target.value > fechaHasta) {
        setFechaHastaError(
          'La fecha hasta no puede ser menor a la fecha desde'
        )
      } else {
        setFechaHastaError('')
      }
    }}
  />
</Field>

<Field label="Período hasta">
  <input
    type="date"
    max={maxFechaFacturacion}
    value={fechaHasta}
    onChange={(event) => {
      const nuevaFechaHasta = event.target.value
      setFechaHasta(nuevaFechaHasta)
      // Validar que no sea menor que fechaDesde
      if (fechaDesde && nuevaFechaHasta < fechaDesde) {
        setFechaHastaError(
          'La fecha hasta no puede ser menor a la fecha desde'
        )
      } else {
        setFechaHastaError('')
      }
      // Validar que no sea mayor que fechaVencimiento
      if (fechaVencimiento && nuevaFechaHasta > fechaVencimiento) {
        setFechaVencimientoError(
          'La fecha de vencimiento no puede ser menor a la fecha hasta'
        )
      } else if (fechaVencimiento && nuevaFechaHasta <= fechaVencimiento) {
        setFechaVencimientoError('')
      }
    }}
  />
  {fechaHastaError && (
    <p className="mt-1 text-sm text-red-500 font-semibold">
      ⚠️ {fechaHastaError}
    </p>
  )}
</Field>

                        <Field label="Fecha de Vto.">
                          <input
                            type="date"
                            value={fechaVencimiento}
                            onChange={(event) => {
                              const nuevaFechaVencimiento = event.target.value
                              setFechaVencimiento(nuevaFechaVencimiento)
                              // Validar que no sea menor a fechaHasta
                              if (fechaHasta && nuevaFechaVencimiento < fechaHasta) {
                                setFechaVencimientoError(
                                  'La fecha de vencimiento no puede ser menor a la fecha hasta'
                                )
                              } else {
                                setFechaVencimientoError('')
                              }
                            }}
                          />
                          {fechaVencimientoError && (
                            <p className="mt-1 text-sm text-red-500 font-semibold">
                              ⚠️ {fechaVencimientoError}
                            </p>
                          )}
                        </Field>
                      </div>
                    </div>
                  )}

                  {/* RESUMEN */}
                  <div className="mt-5 rounded-md border border-emerald-900/10 bg-emerald-50 p-5">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <b className="text-sm text-emerald-950">
                          Factura C
                        </b>

                        <p className="mt-1 text-sm text-emerald-900/65">
                          {finalInvoiceDescription}
                        </p>

                        <p className="mt-2 text-sm font-medium text-emerald-900">
                          Condición frente al IVA del receptor:{' '}
                          {nombreCondicionIVA(condicionIVAReceptor)}
                        </p>

                        {targetKind === 'paciente' && (
                          <p className="mt-2 text-sm font-medium text-emerald-900">
                            Paciente:{' '}
                            {selectedPatient && targetId
                              ? `${selectedPatient.apellido} ${selectedPatient.nombre} — DNI ${selectedPatient.dni}`
                              : 'Sin cargar'}
                          </p>
                        )}

                        {targetKind === 'obra' && (
                          <p className="mt-2 text-sm font-medium text-emerald-900">
                            Paciente:{' '}
                            {selectedPatient && invoicePatientId
                              ? `${selectedPatient.apellido} ${selectedPatient.nombre} — DNI ${selectedPatient.dni}`
                              : 'Sin cargar'}
                          </p>
                        )}
                      </div>

                      <span className="text-2xl font-bold text-emerald-950">
                        $
                        {(() => {
                          let total = 0

                          if (
                            targetKind === 'obra' &&
                            esOsde
                          ) {
                            total = Number(
                              amount.replace(
                                ',',
                                '.'
                              ) || 0
                            )
                          } else {
                            const cantidad = Number(
                              cantidadSesiones.replace(',', '.') || 1
                            )
                            const precio = Number(
                              precioSesion.replace(',', '.') || 0
                            )

                            total = cantidad * precio
                          }

                          return total.toLocaleString(
                            'es-AR'
                          )
                        })()}
                      </span>
                    </div>
                  </div>

                  {/* BOTÓN */}
                  <button
                    type="button"
                    onClick={downloadPdf}
                    className="command mt-5 w-full justify-center"
                  >
                    <ReceiptText size={17} />
                    Facturar y descargar PDF
                  </button>
                </div>
              </div>
            </section>
          )}
        </main>
      </div>

      {/* MODAL PACIENTE */}
      {showPatientForm && (
        <Modal
          title={
            patientId !== null
              ? 'Editar paciente'
              : 'Nuevo paciente'
          }
          onClose={closePatientForm}
        >
          <PatientForm
            data={patient}
            obras={obras}
            setData={setPatient}
            save={savePatient}
            editing={
              patientId !== null
            }
          />
        </Modal>
      )}

      {showPasswordModal && (
        <Modal
          title="Cambiar contraseña"
          onClose={closePasswordModal}
        >
          <form
            onSubmit={handleChangePassword}
            className="space-y-4"
          >
            {[
              {
                label: 'Contraseña actual',
                value: currentPassword,
                setValue: setCurrentPassword,
              },
              {
                label: 'Nueva contraseña',
                value: newPassword,
                setValue: setNewPassword,
              },
              {
                label: 'Confirmar nueva contraseña',
                value: confirmPassword,
                setValue: setConfirmPassword,
              },
            ].map((field) => (
              <Field
                key={field.label}
                label={field.label}
              >
                <div className="login-password-wrap">
                  <input
                    type={
                      showChangePassword
                        ? 'text'
                        : 'password'
                    }
                    autoComplete="new-password"
                    value={field.value}
                    onChange={(event) =>
                      field.setValue(
                        event.target.value
                      )
                    }
                    required
                  />
                  <button
                    type="button"
                    className="login-password-toggle"
                    onClick={() =>
                      setShowChangePassword(
                        (visible) => !visible
                      )
                    }
                    aria-label={
                      showChangePassword
                        ? 'Ocultar contraseñas'
                        : 'Mostrar contraseñas'
                    }
                  >
                    {showChangePassword
                      ? <EyeOff size={18} />
                      : <Eye size={18} />}
                  </button>
                </div>
              </Field>
            ))}

            {passwordMessage && (
              <p
                className={
                  passwordMessage.includes(
                    'correctamente'
                  )
                    ? 'rounded-md bg-emerald-50 p-3 text-sm text-emerald-700'
                    : 'rounded-md bg-red-50 p-3 text-sm text-red-700'
                }
              >
                {passwordMessage}
              </p>
            )}

            <button
              type="submit"
              className="command w-full justify-center"
            >
              <KeyRound size={17} />
              Guardar contraseña
            </button>
          </form>
        </Modal>
      )}

      {/* MODAL OBRA SOCIAL */}
      {showObraForm && (
        <Modal
          title={
            obraId !== null
              ? 'Editar obra social'
              : 'Nueva obra social'
          }
          onClose={closeObraForm}
        >
          <ObraForm
            data={obra}
            setData={setObra}
            save={saveObra}
            editing={obraId !== null}
          />
        </Modal>
      )}
    </div>
  )
}

const Title = ({
  title,
  text,
  button,
}: {
  title: string
  text: string
  button?: React.ReactNode
}) => (
  <div className="flex flex-wrap items-end justify-between gap-4">
    <div>
    
      <h1 className="mt-1 text-4xl font-bold text-emerald-700">
        {title}
      </h1>

      <p className="mt-1 text-sm text-slate-500">
        {text}
      </p>
    </div>

    {button}
  </div>
)

const Field = ({
  label,
  children,
  error,
  isValid,
}: {
  label: string
  children: React.ReactNode
  error?: string | null
  isValid?: boolean
}) => (
  <label className="block text-sm font-medium text-slate-700">
    <div className="mb-1.5 flex items-center justify-between">
      <span>{label}</span>
      {isValid === true && (
        <span className="flex items-center gap-1 text-xs text-green-600">
          <CheckCircle2 size={14} />
          Válido
        </span>
      )}
    </div>

    <div className="relative">
      {children}
      {error && (
        <div className="mt-1.5 flex gap-1.5 rounded-md bg-red-50 p-2 text-xs text-red-700">
          <AlertCircle size={14} className="mt-0.5 shrink-0" />
          <span>{error}</span>
        </div>
      )}
    </div>
  </label>
)

const Modal = ({
  title,
  children,
  onClose,
}: {
  title: string
  children: React.ReactNode
  onClose: () => void
}) => (
  <div
    className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4"
    onMouseDown={(event) => {
      if (
        event.target ===
        event.currentTarget
      ) {
        onClose()
      }
    }}
  >
    <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-xl bg-white shadow-2xl">
      <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-white px-5 py-4">
        <h2 className="text-lg font-bold text-emerald-950">
          {title}
        </h2>

        <button
          type="button"
          onClick={onClose}
          className="icon-button"
          aria-label="Cerrar"
          title="Cerrar"
        >
          <X size={20} />
        </button>
      </div>

      <div className="p-5">
        {children}
      </div>
    </div>
  </div>
)

function PatientsTable({
  items,
  obras,
  edit,
  remove,
}: {
  items: Paciente[]
  obras: Obra[]
  edit: (x: Paciente) => void
  remove: (id: number) => void
}) {
  return (
    <div className="panel overflow-x-auto">
      <table>
        <thead>
          <tr>
            <th>Paciente</th>
            <th>Documento</th>
            <th>Obra social</th>
            <th>Estado</th>
            <th />
          </tr>
        </thead>

        <tbody>
          {items.map((x) => (
            <tr key={x.id}>
              <td>
                <b>
                  {x.apellido}, {x.nombre}
                </b>

                <small>
                  {x.domicilio}
                </small>
              </td>

              <td>
                {x.dni}

                <small>
                  Afiliado {x.numAfiliado}
                </small>
              </td>

              <td>
                {obras.find(
                  (obra) =>
                    obra.id ===
                    x.obraSocialId
                )?.nombre ??
                  'Particular'}
              </td>

              <td>
                <span
                  className={
                    x.estado
                      ? 'badge badge-green'
                      : 'badge'
                  }
                >
                  {x.estado
                    ? 'Activo'
                    : 'Inactivo'}
                </span>
              </td>

              <td className="actions">
                <button
                  type="button"
                  onClick={() =>
                    edit(x)
                  }
                  title="Editar"
                >
                  <Pencil size={16} />
                </button>

                <button
                  type="button"
                  onClick={() =>
                    remove(x.id)
                  }
                  title="Eliminar"
                >
                  <Trash2 size={16} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function ObrasTable({
  items,
  edit,
  remove,
}: {
  items: Obra[]
  edit: (x: Obra) => void
  remove: (id: number) => void
}) {
  return (
    <div className="panel overflow-x-auto">
      <table>
        <thead>
          <tr>
            <th>Obra social</th>
            <th>CUIT</th>
            <th>Condición</th>
            <th>Estado</th>
            <th />
          </tr>
        </thead>

        <tbody>
          {items.map((x) => (
            <tr key={x.id}>
              <td>
                <b>{x.nombre}</b>

                <small>
                  {x.domicilioComercial}
                </small>
              </td>

              <td>{x.cuit}</td>

              <td>
                {x.condicion ===
                'CuentaCorriente'
                  ? 'Cta. cte.'
                  : 'Contado'}
              </td>

              <td>
                <span
                  className={
                    x.estado
                      ? 'badge badge-green'
                      : 'badge'
                  }
                >
                  {x.estado
                    ? 'Activa'
                    : 'Inactiva'}
                </span>
              </td>

              <td className="actions">
                <button
                  type="button"
                  onClick={() =>
                    edit(x)
                  }
                  title="Editar"
                >
                  <Pencil size={16} />
                </button>

                <button
                  type="button"
                  onClick={() =>
                    remove(x.id)
                  }
                  title="Eliminar"
                >
                  <Trash2 size={16} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function PatientForm({
  data,
  setData,
  obras,
  save,
  editing,
}: {
  data: Omit<Paciente, 'id'>
  setData: (
    x: Omit<Paciente, 'id'>
  ) => void
  obras: Obra[]
  save: (
    e: React.FormEvent
  ) => Promise<void>
  editing: boolean
}) {
  const [errors, setErrors] = useState<PacienteErrors>({})

  const handleFieldChange = (field: string, value: string) => {
    setData({
      ...data,
      [field]: value,
    })

    // Validar campo individual
    let fieldError: string | null = null
    switch (field) {
      case 'dni':
        fieldError = validateDni(value)
        break
      case 'nombre':
        fieldError = validateName(value, 'El nombre')
        break
      case 'apellido':
        fieldError = validateName(value, 'El apellido')
        break
      case 'numAfiliado':
        fieldError = validateNumAfiliado(value)
        break
      case 'domicilio':
        fieldError = validateDomicilio(value)
        break
    }

    setErrors((prev) => ({
      ...prev,
      [field as keyof PacienteErrors]: fieldError,
    }))
  }

  const isFormValid =
    !hasErrors(errors) &&
    data.dni &&
    data.nombre &&
    data.apellido &&
    data.numAfiliado &&
    data.domicilio

  const isFieldValid = (field: keyof PacienteErrors): boolean => {
    return !errors[field] && !!data[field]
  }

  return (
    <form
      onSubmit={save}
      className="space-y-4"
    >
      <div className="grid grid-cols-2 gap-3">
        <Field
          label="Nombre"
          error={errors.nombre}
          isValid={isFieldValid('nombre')}
        >
          <input
            required
            value={data.nombre}
            onChange={(event) =>
              handleFieldChange(
                'nombre',
                event.target.value
              )
            }
            className={
              errors.nombre ? 'border-red-300' : ''
            }
          />
        </Field>

        <Field
          label="Apellido"
          error={errors.apellido}
          isValid={isFieldValid('apellido')}
        >
          <input
            required
            value={data.apellido}
            onChange={(event) =>
              handleFieldChange(
                'apellido',
                event.target.value
              )
            }
            className={
              errors.apellido
                ? 'border-red-300'
                : ''
            }
          />
        </Field>
      </div>

      <Field
        label="DNI"
        error={errors.dni}
        isValid={isFieldValid('dni')}
      >
        <input
          required
          value={data.dni}
          onChange={(event) =>
            handleFieldChange(
              'dni',
              event.target.value
            )
          }
          placeholder="12345678"
          className={
            errors.dni ? 'border-red-300' : ''
          }
        />
      </Field>

      <Field
        label="Nº afiliado"
        error={errors.numAfiliado}
        isValid={isFieldValid('numAfiliado')}
      >
        <input
          required
          value={data.numAfiliado}
          onChange={(event) =>
            handleFieldChange(
              'numAfiliado',
              event.target.value
            )
          }
          className={
            errors.numAfiliado
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      <Field
        label="Domicilio"
        error={errors.domicilio}
        isValid={isFieldValid('domicilio')}
      >
        <input
          required
          value={data.domicilio}
          onChange={(event) =>
            handleFieldChange(
              'domicilio',
              event.target.value
            )
          }
          className={
            errors.domicilio
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      {data.obraSocialId == null && (
        <section className="space-y-3 rounded-lg border border-slate-200 p-4">
          <div>
            <h3 className="font-semibold text-slate-800">
              Padre, madre o tutor
            </h3>
            <p className="text-xs text-slate-500">
              Datos opcionales para facturación particular. Quedan asociados a este paciente.
            </p>
          </div>

          <Field label="Vínculo con el paciente">
            <select
              value={data.responsableVinculo ?? ''}
              onChange={(event) =>
                setData({
                  ...data,
                  responsableVinculo: event.target.value,
                })
              }
            >
              <option value="">Seleccionar</option>
              <option value="Madre">Madre</option>
              <option value="Padre">Padre</option>
              <option value="Tutor/a">Tutor/a</option>
            </select>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="Nombre">
              <input
                value={data.responsableNombre ?? ''}
                onChange={(event) =>
                  setData({
                    ...data,
                    responsableNombre: event.target.value,
                  })
                }
              />
            </Field>

            <Field label="Apellido">
              <input
                value={data.responsableApellido ?? ''}
                onChange={(event) =>
                  setData({
                    ...data,
                    responsableApellido: event.target.value,
                  })
                }
              />
            </Field>
          </div>

          <Field label="DNI del responsable">
            <input
              inputMode="numeric"
              maxLength={8}
              value={data.responsableDni ?? ''}
              onChange={(event) =>
                setData({
                  ...data,
                  responsableDni: event.target.value,
                })
              }
              placeholder="12345678"
            />
          </Field>

          <Field label="Domicilio del responsable">
            <input
              maxLength={200}
              value={data.responsableDomicilio ?? ''}
              onChange={(event) =>
                setData({
                  ...data,
                  responsableDomicilio: event.target.value,
                })
              }
            />
          </Field>

          <Field label="Condición frente al IVA del responsable">
            <select
              value={data.responsableCondicionIVA ?? ''}
              onChange={(event) =>
                setData({
                  ...data,
                  responsableCondicionIVA:
                    (event.target.value as CondicionIVA) || null,
                })
              }
            >
              <option value="">Seleccionar condición</option>
              {condicionesIVA.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </Field>
        </section>
      )}

      <Field label="Obra social">
        <select
          value={
            data.obraSocialId ?? ''
          }
          onChange={(event) =>
            setData({
              ...data,
              obraSocialId:
                event.target.value
                  ? Number(
                      event.target.value
                    )
                  : null,
            })
          }
        >
          <option value="">
            Particular
          </option>

          {obras.map((x) => (
            <option
              key={x.id}
              value={x.id}
            >
              {x.nombre}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Observaciones">
        <textarea
          value={data.observaciones ?? ''}
          onChange={(event) =>
            setData({
              ...data,
              observaciones:
                event.target.value,
            })
          }
          rows={4}
          placeholder="Observaciones del paciente..."
        />
      </Field>

      <label className="flex gap-2 text-sm">
        <input
          type="checkbox"
          checked={data.estado}
          onChange={(event) =>
            setData({
              ...data,
              estado:
                event.target.checked,
            })
          }
        />

        Activo
      </label>

      <button
        type="submit"
        disabled={!isFormValid}
        className="command w-full justify-center disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {editing
          ? 'Guardar cambios'
          : 'Guardar paciente'}
      </button>
    </form>
  )
}

function ObraForm({
  data,
  setData,
  save,
  editing,
}: {
  data: Omit<Obra, 'id'>
  setData: (
    x: Omit<Obra, 'id'>
  ) => void
  save: (
    e: React.FormEvent
  ) => Promise<void>
  editing: boolean
}) {
  const [errors, setErrors] = useState<ObraErrors>({})

  const handleFieldChange = (field: string, value: string) => {
    setData({
      ...data,
      [field]: value,
    } as Omit<Obra, 'id'>)

    // Validar campo individual
    let fieldError: string | null = null
    switch (field) {
      case 'cuit':
        fieldError = validateCuit(value)
        break
      case 'nombre':
        fieldError = validateName(value, 'El nombre')
        break
      case 'domicilioComercial':
        fieldError = validateDomicilio(value)
        break
      case 'mail':
        fieldError = validateEmail(value)
        break
    }

    setErrors((prev) => ({
      ...prev,
      [field as keyof ObraErrors]: fieldError,
    }))
  }

  const isFormValid =
    !hasErrors(errors) &&
    data.cuit &&
    data.nombre &&
    data.domicilioComercial &&
    data.condicionIVA

  const isFieldValid = (field: keyof ObraErrors): boolean => {
    return !errors[field] && !!data[field as keyof typeof data]
  }

  return (
    <form
      onSubmit={save}
      className="space-y-4"
    >
      <Field
        label="Nombre"
        error={errors.nombre}
        isValid={isFieldValid('nombre')}
      >
        <input
          required
          value={data.nombre}
          onChange={(event) =>
            handleFieldChange(
              'nombre',
              event.target.value
            )
          }
          className={
            errors.nombre
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      <Field
        label="CUIT"
        error={errors.cuit}
        isValid={isFieldValid('cuit')}
      >
        <input
          required
          value={data.cuit}
          onChange={(event) =>
            handleFieldChange(
              'cuit',
              event.target.value
            )
          }
          placeholder="12345678901"
          className={
            errors.cuit
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      <Field
        label="Domicilio comercial"
        error={errors.domicilioComercial}
        isValid={isFieldValid(
          'domicilioComercial'
        )}
      >
        <input
          required
          value={
            data.domicilioComercial
          }
          onChange={(event) =>
            handleFieldChange(
              'domicilioComercial',
              event.target.value
            )
          }
          className={
            errors.domicilioComercial
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      <Field
        label="Mail"
        error={errors.mail}
        isValid={
          !errors.mail &&
          !!data.mail
        }
      >
        <input
          type="email"
          value={data.mail ?? ''}
          onChange={(event) =>
            handleFieldChange(
              'mail',
              event.target.value
            )
          }
          placeholder="contacto@ejemplo.com"
          className={
            errors.mail
              ? 'border-red-300'
              : ''
          }
        />
      </Field>

      <Field label="Condición frente al IVA">
        <select
          required
          value={data.condicionIVA ?? ''}
          onChange={(event) =>
            setData({
              ...data,
              condicionIVA:
                (event.target.value as CondicionIVA) || null,
              condicionIVAConfirmada: true,
            })
          }
        >
          <option value="">Seleccionar condición</option>
          {condicionesIVA.map((item) => (
            <option key={item.value} value={item.value}>
              {item.label}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Condición">
        <select
          value={data.condicion}
          onChange={(event) =>
            setData({
              ...data,
              condicion:
                event.target
                  .value as Obra['condicion'],
            })
          }
        >
          <option value="Contado">
            Contado
          </option>

          <option value="CuentaCorriente">
            Cuenta corriente
          </option>
        </select>
      </Field>

      <label className="flex gap-2 text-sm">
        <input
          type="checkbox"
          checked={data.estado}
          onChange={(event) =>
            setData({
              ...data,
              estado:
                event.target.checked,
            })
          }
        />

        Activa
      </label>

      <button
        type="submit"
        disabled={!isFormValid}
        className="command w-full justify-center disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {editing
          ? 'Guardar cambios'
          : 'Guardar obra social'}
      </button>
    </form>
  )
}

export default App