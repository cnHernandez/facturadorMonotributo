
import { useEffect, useState } from 'react'
import {
  Building2,
  FileText,
  HeartPulse,
  Pencil,
  Plus,
  ReceiptText,
  Trash2,
  Users,
  X,
} from 'lucide-react'

type Paciente = {
  id: number
  dni: string
  numAfiliado: string
  nombre: string
  apellido: string
  domicilio: string
  estado: boolean
  obraSocialId?: number | null
}

type Obra = {
  id: number
  cuit: string
  nombre: string
  domicilioComercial: string
  condicion: 'Contado' | 'CuentaCorriente'
  estado: boolean
}

type View = 'pacientes' | 'obras' | 'facturar'

const patientBlank = (): Omit<Paciente, 'id'> => ({
  dni: '',
  numAfiliado: '',
  nombre: '',
  apellido: '',
  domicilio: '',
  estado: true,
  obraSocialId: null,
})

const obraBlank = (): Omit<Obra, 'id'> => ({
  cuit: '',
  nombre: '',
  domicilioComercial: '',
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
    },
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`)
  }

  return response.status === 204
    ? null
    : response.json()
}

function App() {
  const [view, setView] = useState<View>('pacientes')
  const [message, setMessage] = useState('Cargando datos...')

  const [pacientes, setPacientes] = useState<Paciente[]>([])
  const [obras, setObras] = useState<Obra[]>([])

  const [patient, setPatient] =
    useState<Omit<Paciente, 'id'>>(patientBlank())

  const [obra, setObra] =
    useState<Omit<Obra, 'id'>>(obraBlank())

  const [patientId, setPatientId] = useState<number | null>(null)
  const [obraId, setObraId] = useState<number | null>(null)

  const [showPatientForm, setShowPatientForm] = useState(false)
  const [showObraForm, setShowObraForm] = useState(false)

  const [targetKind, setTargetKind] =
    useState<'paciente' | 'obra'>('paciente')

  const [targetId, setTargetId] = useState('')
  const [amount, setAmount] = useState('1000')

  useEffect(() => {
    Promise.all([
      api('/api/pacientes', 'GET'),
      api('/api/obras-sociales', 'GET'),
    ])
      .then(([patientData, obraData]) => {
        setPacientes(patientData || [])
        setObras(obraData || [])

        if (patientData?.length) {
          setTargetKind('paciente')
          setTargetId(String(patientData[0].id))
        } else if (obraData?.length) {
          setTargetKind('obra')
          setTargetId(String(obraData[0].id))
        }

        setMessage(
          patientData?.length
            ? 'Datos cargados correctamente'
            : 'Base de datos vacía'
        )
      })
      .catch((error) => {
        setMessage(
          'Error conectando con API: ' +
            (error?.message || 'Error desconocido')
        )
      })
  }, [])

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

  const savePatient = async (event: React.FormEvent) => {
    event.preventDefault()

    try {
      const data = await api(
        patientId
          ? `/api/pacientes/${patientId}`
          : '/api/pacientes',
        patientId ? 'PUT' : 'POST',
        patient
      )

      setPacientes((items) =>
        patientId
          ? items.map((x) =>
              x.id === patientId
                ? data ?? { ...patient, id: patientId }
                : x
            )
          : [
              ...items,
              data ?? {
                ...patient,
                id: Date.now(),
              },
            ]
      )

      setMessage(
        patientId
          ? 'Paciente actualizado correctamente'
          : 'Paciente creado correctamente'
      )

      closePatientForm()
    } catch (error) {
      console.error(error)
      setMessage('No se pudo guardar el paciente')
    }
  }

  const saveObra = async (event: React.FormEvent) => {
    event.preventDefault()

    try {
      const data = await api(
        obraId
          ? `/api/obras-sociales/${obraId}`
          : '/api/obras-sociales',
        obraId ? 'PUT' : 'POST',
        obra
      )

      setObras((items) =>
        obraId
          ? items.map((x) =>
              x.id === obraId
                ? data ?? { ...obra, id: obraId }
                : x
            )
          : [
              ...items,
              data ?? {
                ...obra,
                id: Date.now(),
              },
            ]
      )

      setMessage(
        obraId
          ? 'Obra social actualizada correctamente'
          : 'Obra social creada correctamente'
      )

      closeObraForm()
    } catch (error) {
      console.error(error)
      setMessage('No se pudo guardar la obra social')
    }
  }

  const remove = async (
    kind: 'pacientes' | 'obras-sociales',
    id: number
  ) => {
    if (!confirm('¿Eliminar este registro?')) return

    try {
      await api(`/api/${kind}/${id}`, 'DELETE')

      if (kind === 'pacientes') {
        setPacientes((items) =>
          items.filter((item) => item.id !== id)
        )

        if (targetKind === 'paciente' && targetId === String(id)) {
          const remaining = pacientes.filter(
            (item) => item.id !== id
          )

          setTargetId(
            remaining.length
              ? String(remaining[0].id)
              : ''
          )
        }
      } else {
        setObras((items) =>
          items.filter((item) => item.id !== id)
        )

        if (targetKind === 'obra' && targetId === String(id)) {
          const remaining = obras.filter(
            (item) => item.id !== id
          )

          setTargetId(
            remaining.length
              ? String(remaining[0].id)
              : ''
          )
        }
      }

      setMessage('Registro eliminado correctamente')
    } catch (error) {
      console.error(error)
      setMessage('No se pudo eliminar el registro')
    }
  }

  const downloadPdf = async () => {
    const selected =
      targetKind === 'paciente'
        ? pacientes.find(
            (x) => x.id === Number(targetId)
          )
        : obras.find(
            (x) => x.id === Number(targetId)
          )

    if (!selected) {
      setMessage('Seleccioná un destinatario para facturar')
      return
    }

    const numericAmount = Number(
      amount.replace(',', '.')
    )

    if (!Number.isFinite(numericAmount) || numericAmount <= 0) {
      setMessage('Ingresá un importe válido')
      return
    }

    const isPatient = 'dni' in selected

    try {
      setMessage('Emitiendo factura...')

      const response = await fetch(
        '/api/arca/cae/pdf',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            tipoComprobante: 11,
            tipoDocumentoReceptor: isPatient
              ? 96
              : 80,
            numeroDocumentoReceptor: Number(
              (
                isPatient
                  ? selected.dni
                  : selected.cuit
              ).replaceAll('-', '')
            ),
            nombreReceptor: isPatient
              ? `${selected.nombre} ${selected.apellido}`
              : selected.nombre,
            importeTotal: numericAmount,
          }),
        }
      )

      if (!response.ok) {
        throw new Error(
          `HTTP ${response.status}`
        )
      }

      const blob = await response.blob()
      const url = URL.createObjectURL(blob)

      const link =
        document.createElement('a')

      link.href = url
      link.download = 'factura-c.pdf'

      document.body.appendChild(link)
      link.click()
      link.remove()

      URL.revokeObjectURL(url)

      setMessage(
        'Factura emitida y descargada correctamente'
      )
    } catch (error) {
      console.error(error)
      setMessage('No se pudo emitir la factura')
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

  const targets =
    targetKind === 'paciente'
      ? pacientes
      : obras

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

        <span className="hidden text-xs text-slate-500 sm:block">
          {message}
        </span>
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
                onClick={() => setView(item.id)}
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
                  remove('pacientes', id)
                }
              />
            </section>
          )}

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
                  remove('obras-sociales', id)
                }
              />
            </section>
          )}

          {view === 'facturar' && (
            <section className="space-y-6">
              <Title
                title="Facturar"
                text="Emisión de comprobantes electrónicos"
              />

              <div className="w-full">
                <div className="panel p-6">
                  <div className="grid gap-5 sm:grid-cols-2">
                    <Field
                      label={
                        targetKind === 'paciente'
                          ? 'Paciente'
                          : 'Obra social'
                      }
                    >
                      <select
                        value={targetId}
                        onChange={(event) =>
                          setTargetId(
                            event.target.value
                          )
                        }
                      >
                        {targets.map((x) => (
                          <option
                            key={x.id}
                            value={x.id}
                          >
                            {'apellido' in x
                              ? `${x.apellido}, ${x.nombre}`
                              : x.nombre}
                          </option>
                        ))}
                      </select>
                    </Field>

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
                  </div>

                  <div className="mt-5 flex gap-2 border-b border-slate-200 pb-2">
                    <button
                      type="button"
                      onClick={() => {
                        setTargetKind('paciente')
                        setTargetId(
                          String(
                            pacientes[0]?.id ?? ''
                          )
                        )
                      }}
                      className={`tab ${
                        targetKind === 'paciente'
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
                        setTargetKind('obra')
                        setTargetId(
                          String(
                            obras[0]?.id ?? ''
                          )
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

                  <div className="mt-5 rounded-md border border-emerald-900/10 bg-emerald-50 p-4">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <b className="text-sm text-emerald-950">
                          Factura C
                        </b>

                        <p className="mt-1 text-sm text-emerald-900/65">
                          Servicios profesionales
                          psicopedagógicos
                        </p>
                      </div>

                      <span className="text-xl font-bold text-emerald-950">
                        $
                        {Number(
                          amount
                            .replace(',', '.') ||
                            0
                        ).toLocaleString('es-AR')}
                      </span>
                    </div>
                  </div>

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
            editing={patientId !== null}
          />
        </Modal>
      )}

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
}: {
  label: string
  children: React.ReactNode
}) => (
  <label className="block text-sm font-medium text-slate-700">
    <span className="mb-1.5 block">
      {label}
    </span>

    {children}
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
      if (event.target === event.currentTarget) {
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
                  (o) =>
                    o.id === x.obraSocialId
                )?.nombre ?? 'Particular'}
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
                  onClick={() => edit(x)}
                  title="Editar"
                >
                  <Pencil size={16} />
                </button>

                <button
                  type="button"
                  onClick={() => remove(x.id)}
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
                  onClick={() => edit(x)}
                  title="Editar"
                >
                  <Pencil size={16} />
                </button>

                <button
                  type="button"
                  onClick={() => remove(x.id)}
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
  return (
    <form
      onSubmit={save}
      className="space-y-4"
    >
      <div className="grid grid-cols-2 gap-3">
        <Field label="Nombre">
          <input
            required
            value={data.nombre}
            onChange={(event) =>
              setData({
                ...data,
                nombre: event.target.value,
              })
            }
          />
        </Field>

        <Field label="Apellido">
          <input
            required
            value={data.apellido}
            onChange={(event) =>
              setData({
                ...data,
                apellido:
                  event.target.value,
              })
            }
          />
        </Field>
      </div>

      <Field label="DNI">
        <input
          required
          value={data.dni}
          onChange={(event) =>
            setData({
              ...data,
              dni: event.target.value,
            })
          }
        />
      </Field>

      <Field label="Nº afiliado">
        <input
          required
          value={data.numAfiliado}
          onChange={(event) =>
            setData({
              ...data,
              numAfiliado:
                event.target.value,
            })
          }
        />
      </Field>

      <Field label="Domicilio">
        <input
          required
          value={data.domicilio}
          onChange={(event) =>
            setData({
              ...data,
              domicilio:
                event.target.value,
            })
          }
        />
      </Field>

      <Field label="Obra social">
        <select
          value={data.obraSocialId ?? ''}
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
        className="command w-full justify-center"
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
  return (
    <form
      onSubmit={save}
      className="space-y-4"
    >
      <Field label="Nombre">
        <input
          required
          value={data.nombre}
          onChange={(event) =>
            setData({
              ...data,
              nombre: event.target.value,
            })
          }
        />
      </Field>

      <Field label="CUIT">
        <input
          required
          value={data.cuit}
          onChange={(event) =>
            setData({
              ...data,
              cuit: event.target.value,
            })
          }
        />
      </Field>

      <Field label="Domicilio comercial">
        <input
          required
          value={data.domicilioComercial}
          onChange={(event) =>
            setData({
              ...data,
              domicilioComercial:
                event.target.value,
            })
          }
        />
      </Field>

      <Field label="Condición">
        <select
          value={data.condicion}
          onChange={(event) =>
            setData({
              ...data,
              condicion:
                event.target.value as Obra['condicion'],
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
        className="command w-full justify-center"
      >
        {editing
          ? 'Guardar cambios'
          : 'Guardar obra social'}
      </button>
    </form>
  )
}

export default App

