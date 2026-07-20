import { useMemo, useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, KeyRound, Pencil, Plus, Search, X } from 'lucide-react'
import {
  clientsApi,
  courtTypesApi,
  courtsApi,
  employeesApi,
  promotionsApi,
  rolesApi,
  turnsApi,
  usersApi,
  type CourtPayload,
  type PersonPayload,
  type PromotionPayload,
  type TurnPayload,
  type UserCreatePayload,
  type UserUpdatePayload,
} from '@/api/catalogs.api'
import { ApiRequestError } from '@/api/http'
import {
  courtSchema,
  courtTypeSchema,
  passwordSchema,
  personSchema,
  promotionSchema,
  toFieldErrors,
  turnSchema,
  userSchema,
  userUpdateSchema,
  type FieldErrors,
} from '@/lib/validation'
import type { AvailableTurn, Client, Court, CourtType, Employee, Promotion, RoleCatalog, UserCatalog } from '@/types/api'

type StatusItem = { id: number; isActive: boolean }
type FormMode<T> = { item: T | null } | null

const emptyPerson: PersonPayload = { firstName: '', lastName: '', dni: '', phone: '', email: '' }

function getErrorMessage(error: unknown) {
  return error instanceof ApiRequestError ? error.message : 'No se pudo completar la operacion.'
}

function activeBadge(isActive: boolean) {
  return (
    <span className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold ${isActive ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-slate-100 text-slate-500'}`}>
      {isActive ? 'Activo' : 'Inactivo'}
    </span>
  )
}

function PageShell({
  actionLabel,
  children,
  description,
  onCreate,
  title,
}: {
  actionLabel: string
  children: ReactNode
  description: string
  onCreate: () => void
  title: string
}) {
  return (
    <section className="space-y-4 rounded-2xl border border-[#E2E8F0]/80 bg-white/55 p-5 shadow-[0_18px_46px_rgba(15,23,42,0.05)]">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="inline-flex rounded-full border border-teal-200 bg-teal-50 px-3 py-1 text-sm font-semibold text-[#0F766E]">Etapa 3</p>
          <h3 className="mt-2 text-2xl font-semibold tracking-normal text-[#0F172A]">{title}</h3>
          <p className="mt-1 max-w-2xl text-sm leading-6 text-[#475569]">{description}</p>
        </div>
        <button className="inline-flex translate-y-0 items-center gap-2 rounded-xl bg-[linear-gradient(135deg,#0F766E_0%,#7C3AED_100%)] px-4 py-2.5 text-sm font-semibold text-white shadow-[0_12px_24px_rgba(15,118,110,0.22)] transition duration-200 hover:-translate-y-px hover:brightness-110 active:translate-y-0" onClick={onCreate} type="button">
          <Plus aria-hidden="true" className="size-4" strokeWidth={2.2} />
          {actionLabel}
        </button>
      </div>
      {children}
    </section>
  )
}

function SearchBox({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  return (
    <label className="flex w-full max-w-sm items-center gap-2 rounded-xl border border-[#CBD5E1] bg-white px-3 py-2.5 text-sm text-[#475569] shadow-[0_8px_20px_rgba(15,23,42,0.04)] transition focus-within:border-[#7C3AED] focus-within:ring-4 focus-within:ring-teal-500/10">
      <Search aria-hidden="true" className="size-4 shrink-0 text-[#334155]" strokeWidth={1.9} />
      <input className="w-full bg-transparent text-sm text-[#0F172A] outline-none placeholder:text-[#94A3B8]" onChange={(event) => onChange(event.target.value)} placeholder="Buscar" value={value} />
    </label>
  )
}

function Panel({ children }: { children: ReactNode }) {
  return <div className="overflow-hidden rounded-2xl border border-[#E2E8F0] bg-white shadow-[0_14px_35px_rgba(15,23,42,0.06)]">{children}</div>
}

function FormControl({ children, error, label, className = '' }: { children: ReactNode; error?: string; label: string; className?: string }) {
  return <label className={`grid gap-1 text-sm font-medium text-slate-700 ${className}`}><span>{label}</span>{children}{error ? <span className="text-xs font-medium text-red-600" role="alert">{error}</span> : null}</label>
}

function ActionButton({ children, onClick, title }: { children: React.ReactNode; onClick: () => void; title: string }) {
  return (
    <button className="inline-flex size-8 items-center justify-center rounded-lg border border-[#CBD5E1] bg-white text-[#334155] shadow-[0_6px_14px_rgba(15,23,42,0.04)] transition duration-200 hover:-translate-y-px hover:border-[#0F766E]/30 hover:bg-[#F8FAFC] hover:text-[#0F766E] active:translate-y-0" onClick={onClick} title={title} type="button">
      {children}
    </button>
  )
}

function StatusActions<TItem extends StatusItem>({
  item,
  onEdit,
  onToggle,
}: {
  item: TItem
  onEdit: (item: TItem) => void
  onToggle: (item: TItem) => void
}) {
  return (
    <div className="flex justify-end gap-2">
      <ActionButton onClick={() => onEdit(item)} title="Editar">
        <Pencil aria-hidden="true" className="size-4" />
      </ActionButton>
      <ActionButton onClick={() => onToggle(item)} title={item.isActive ? 'Desactivar' : 'Activar'}>
        {item.isActive ? <X aria-hidden="true" className="size-4" /> : <Check aria-hidden="true" className="size-4" />}
      </ActionButton>
    </div>
  )
}

function PersonForm({
  error,
  initial,
  isSaving,
  onCancel,
  onSubmit,
  title,
}: {
  error?: string
  initial: PersonPayload
  isSaving: boolean
  onCancel: () => void
  onSubmit: (payload: PersonPayload) => void
  title: string
}) {
  const [form, setForm] = useState<PersonPayload>(initial)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const update = (key: keyof PersonPayload, value: string) => setForm((current) => ({ ...current, [key]: value }))

  return (
    <form
      className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-2"
      onSubmit={(event) => {
        event.preventDefault()
        const parsed = personSchema.safeParse(form)
        if (!parsed.success) {
          setFieldErrors(toFieldErrors(parsed.error))
          return
        }
        setFieldErrors({})
        onSubmit(parsed.data)
      }}
    >
      <h4 className="md:col-span-2 text-sm font-semibold text-slate-900">{title}</h4>
      <FormControl error={fieldErrors.firstName} label="Nombre"><input aria-invalid={Boolean(fieldErrors.firstName)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={60} onChange={(event) => update('firstName', event.target.value)} value={form.firstName} /></FormControl>
      <FormControl error={fieldErrors.lastName} label="Apellido"><input aria-invalid={Boolean(fieldErrors.lastName)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={60} onChange={(event) => update('lastName', event.target.value)} value={form.lastName} /></FormControl>
      <FormControl error={fieldErrors.dni} label="DNI"><input aria-invalid={Boolean(fieldErrors.dni)} inputMode="numeric" required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={10} onChange={(event) => update('dni', event.target.value)} value={form.dni} /></FormControl>
      <FormControl error={fieldErrors.phone} label="Teléfono"><input aria-invalid={Boolean(fieldErrors.phone)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={40} onChange={(event) => update('phone', event.target.value)} type="tel" value={form.phone} /></FormControl>
      <FormControl className="md:col-span-2" error={fieldErrors.email} label="Email"><input aria-invalid={Boolean(fieldErrors.email)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={120} onChange={(event) => update('email', event.target.value)} type="email" value={form.email} /></FormControl>
      {error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}
      <div className="flex justify-end gap-2 md:col-span-2">
        <button className="rounded-md border border-slate-200 px-3 py-2 text-sm font-medium" onClick={onCancel} type="button">Cancelar</button>
        <button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} type="submit">Guardar</button>
      </div>
    </form>
  )
}

function toPersonPayload(item?: Client | Employee | null): PersonPayload {
  return item
    ? { firstName: item.firstName, lastName: item.lastName, dni: item.dni ?? '', phone: item.phone ?? '', email: item.email ?? '' }
    : emptyPerson
}

function PeopleTable<TItem extends Client | Employee>({
  items,
  kind,
  onEdit,
  onToggle,
}: {
  items: TItem[]
  kind: 'clients' | 'employees'
  onEdit: (item: TItem) => void
  onToggle: (item: TItem) => void
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="bg-[#F8FAFC] text-xs uppercase text-[#334155]">
          <tr>
            <th className="px-4 py-3 font-bold">Persona</th>
            <th className="px-4 py-3 font-bold">DNI</th>
            <th className="px-4 py-3 font-bold">Contacto</th>
            {kind === 'employees' ? <th className="px-4 py-3 font-bold">Usuario</th> : null}
            <th className="px-4 py-3 font-bold">Estado</th>
            <th className="px-4 py-3 text-right font-bold">Acciones</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-[#E2E8F0]">
          {items.map((item) => (
            <tr className="transition hover:bg-[#F8FAFC]" key={item.id}>
              <td className="px-4 py-3 font-medium text-[#0F172A]">{item.firstName} {item.lastName}</td>
              <td className="px-4 py-3 text-[#334155]">{item.dni ?? '-'}</td>
              <td className="px-4 py-3 text-[#334155]">{item.phone ?? item.email ?? '-'}</td>
              {kind === 'employees' ? <td className="px-4 py-3 text-[#334155]">{(item as Employee).hasUser ? 'Asignado' : 'Sin usuario'}</td> : null}
              <td className="px-4 py-3">{activeBadge(item.isActive)}</td>
              <td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function PeoplePage<TItem extends Client | Employee>({
  api,
  description,
  kind,
  queryKey,
  title,
}: {
  api: {
    list: () => Promise<TItem[]>
    create: (payload: PersonPayload) => Promise<TItem>
    update: (id: number, payload: PersonPayload) => Promise<TItem>
    activate: (id: number) => Promise<void>
    deactivate: (id: number) => Promise<void>
  }
  description: string
  kind: 'clients' | 'employees'
  queryKey: string
  title: string
}) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [formMode, setFormMode] = useState<FormMode<TItem>>(null)
  const [error, setError] = useState<string>()
  const query = useQuery({ queryKey: [queryKey], queryFn: api.list })
  const saveMutation = useMutation({
    mutationFn: (payload: PersonPayload) => formMode?.item ? api.update(formMode.item.id, payload) : api.create(payload),
    onSuccess: async () => {
      setFormMode(null)
      setError(undefined)
      await queryClient.invalidateQueries({ queryKey: [queryKey] })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })
  const toggleMutation = useMutation({
    mutationFn: (item: TItem) => item.isActive ? api.deactivate(item.id) : api.activate(item.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [queryKey] }),
  })
  const items = useMemo(() => (query.data ?? []).filter((item) => `${item.firstName} ${item.lastName} ${item.dni ?? ''}`.toLowerCase().includes(search.toLowerCase())), [query.data, search])

  return (
    <PageShell actionLabel={`Nuevo ${kind === 'clients' ? 'cliente' : 'empleado'}`} description={description} onCreate={() => setFormMode({ item: null })} title={title}>
      <SearchBox onChange={setSearch} value={search} />
      <Panel>
        {formMode ? <PersonForm error={error} initial={toPersonPayload(formMode.item)} isSaving={saveMutation.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => saveMutation.mutate(payload)} title={formMode.item ? 'Editar registro' : 'Nuevo registro'} /> : null}
        {query.isLoading ? <p className="p-4 text-sm text-slate-500">Cargando...</p> : <PeopleTable items={items} kind={kind} onEdit={(item) => setFormMode({ item })} onToggle={(item) => toggleMutation.mutate(item)} />}
      </Panel>
    </PageShell>
  )
}

export function ClientsPage() {
  return <PeoplePage api={clientsApi} description="Gestion de jugadores y datos de contacto para futuras reservas." kind="clients" queryKey="clients" title="Clientes" />
}

export function EmployeesPage() {
  return <PeoplePage api={employeesApi} description="Administracion del personal interno del club." kind="employees" queryKey="employees" title="Empleados" />
}

export function UsersPage() {
  const queryClient = useQueryClient()
  const [formMode, setFormMode] = useState<FormMode<UserCatalog>>(null)
  const [passwordUser, setPasswordUser] = useState<UserCatalog | null>(null)
  const [error, setError] = useState<string>()
  const usersQuery = useQuery({ queryKey: ['users'], queryFn: usersApi.list })
  const employeesQuery = useQuery({ queryKey: ['employees'], queryFn: employeesApi.list })
  const rolesQuery = useQuery({ queryKey: ['roles'], queryFn: rolesApi.list })
  const saveMutation = useMutation({
    mutationFn: (payload: UserCreatePayload | UserUpdatePayload) => formMode?.item ? usersApi.update(formMode.item.id, payload as UserUpdatePayload) : usersApi.create(payload as UserCreatePayload),
    onSuccess: async () => {
      setFormMode(null)
      setError(undefined)
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      await queryClient.invalidateQueries({ queryKey: ['employees'] })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })
  const toggleMutation = useMutation({ mutationFn: (item: UserCatalog) => item.isActive ? usersApi.deactivate(item.id) : usersApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }) })

  return (
    <PageShell actionLabel="Nuevo usuario" description="Alta, roles y estado de usuarios internos." onCreate={() => setFormMode({ item: null })} title="Usuarios">
      <Panel>
        {formMode ? <UserForm employees={employeesQuery.data ?? []} error={error} initial={formMode.item} isSaving={saveMutation.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => saveMutation.mutate(payload)} roles={rolesQuery.data ?? []} /> : null}
        {passwordUser ? <PasswordForm error={error} isSaving={saveMutation.isPending} onCancel={() => setPasswordUser(null)} onSubmit={(password) => usersApi.changePassword(passwordUser.id, password).then(async () => { setPasswordUser(null); await queryClient.invalidateQueries({ queryKey: ['users'] }) }).catch((mutationError: unknown) => setError(getErrorMessage(mutationError)))} username={passwordUser.username} /> : null}
        {usersQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Cargando...</p> : <UsersTable items={usersQuery.data ?? []} onEdit={(item) => setFormMode({ item })} onPassword={setPasswordUser} onToggle={(item) => toggleMutation.mutate(item)} />}
      </Panel>
    </PageShell>
  )
}

function UserForm({ employees, error, initial, isSaving, onCancel, onSubmit, roles }: { employees: Employee[]; error?: string; initial: UserCatalog | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: UserCreatePayload | UserUpdatePayload) => void; roles: RoleCatalog[] }) {
  const availableEmployees = employees.filter((employee) => employee.isActive && (!employee.hasUser || employee.id === initial?.employeeId))
  const [username, setUsername] = useState(initial?.username ?? '')
  const [password, setPassword] = useState('')
  const [employeeId, setEmployeeId] = useState(initial?.employeeId ?? availableEmployees[0]?.id ?? 0)
  const [roleId, setRoleId] = useState(initial?.roleId ?? roles[0]?.id ?? 1)
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})

  const submit = (event: React.FormEvent) => {
    event.preventDefault()
    const values = initial ? { username, roleId } : { username, password, employeeId, roleId }
    const parsed = (initial ? userUpdateSchema : userSchema).safeParse(values)
    if (!parsed.success) {
      setValidationErrors(toFieldErrors(parsed.error))
      return
    }
    setValidationErrors({})
    onSubmit(parsed.data)
  }

  return (
    <form className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-2" onSubmit={submit}>
      <h4 className="md:col-span-2 text-sm font-semibold">{initial ? 'Editar usuario' : 'Nuevo usuario'}</h4>
      <FormControl error={validationErrors.username} label="Username"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={50} onChange={(event) => setUsername(event.target.value)} value={username} /></FormControl>
      {!initial ? <FormControl error={validationErrors.password} label="Contraseña"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" minLength={8} maxLength={100} onChange={(event) => setPassword(event.target.value)} type="password" value={password} /></FormControl> : null}
      {!initial ? <FormControl error={validationErrors.employeeId} label="Empleado"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setEmployeeId(Number(event.target.value))} value={employeeId || ''}><option value="">Seleccionar empleado</option>{availableEmployees.map((employee) => <option key={employee.id} value={employee.id}>{employee.firstName} {employee.lastName}</option>)}</select></FormControl> : <p className="rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-600">{initial.employeeName}</p>}
      <FormControl error={validationErrors.roleId} label="Rol"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setRoleId(Number(event.target.value))} value={roleId || ''}><option value="">Seleccionar rol</option>{roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}</select></FormControl>
      {error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}
      <div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button></div>
    </form>
  )
}

function PasswordForm({ error, isSaving, onCancel, onSubmit, username }: { error?: string; isSaving: boolean; onCancel: () => void; onSubmit: (password: string) => void; username: string }) {
  const [password, setPassword] = useState('')
  const [validationError, setValidationError] = useState<string>()
  return <form className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-2" onSubmit={(event) => { event.preventDefault(); const parsed = passwordSchema.safeParse(password); if (!parsed.success) { setValidationError(parsed.error.issues[0]?.message); return } setValidationError(undefined); onSubmit(parsed.data) }}><h4 className="md:col-span-2 text-sm font-semibold">Cambiar password de {username}</h4><FormControl error={validationError} label="Nueva contraseña"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" minLength={8} maxLength={100} onChange={(event) => setPassword(event.target.value)} type="password" value={password} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button></div></form>
}

function UsersTable({ items, onEdit, onPassword, onToggle }: { items: UserCatalog[]; onEdit: (item: UserCatalog) => void; onPassword: (item: UserCatalog) => void; onToggle: (item: UserCatalog) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Usuario</th><th className="px-4 py-3">Empleado</th><th className="px-4 py-3">Rol</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.username}</td><td className="px-4 py-3 text-slate-600">{item.employeeName}</td><td className="px-4 py-3 text-slate-600">{item.role}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><div className="flex justify-end gap-2"><ActionButton onClick={() => onEdit(item)} title="Editar"><Pencil aria-hidden="true" className="size-4" /></ActionButton><ActionButton onClick={() => onPassword(item)} title="Cambiar password"><KeyRound aria-hidden="true" className="size-4" /></ActionButton><ActionButton onClick={() => onToggle(item)} title={item.isActive ? 'Desactivar' : 'Activar'}>{item.isActive ? <X aria-hidden="true" className="size-4" /> : <Check aria-hidden="true" className="size-4" />}</ActionButton></div></td></tr>)}</tbody></table></div>
}

export function CourtsPage() {
  return <CourtsManagementPage />
}

function CourtsManagementPage() {
  const queryClient = useQueryClient()
  const [courtForm, setCourtForm] = useState<FormMode<Court>>(null)
  const [typeForm, setTypeForm] = useState<CourtType | null | 'new'>(null)
  const [error, setError] = useState<string>()
  const courtsQuery = useQuery({ queryKey: ['courts'], queryFn: courtsApi.list })
  const typesQuery = useQuery({ queryKey: ['court-types'], queryFn: courtTypesApi.list })
  const courtSave = useMutation({ mutationFn: (payload: CourtPayload) => courtForm?.item ? courtsApi.update(courtForm.item.id, payload) : courtsApi.create(payload), onSuccess: async () => { setCourtForm(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['courts'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const typeSave = useMutation({ mutationFn: (description: string) => typeForm && typeForm !== 'new' ? courtTypesApi.update(typeForm.id, description) : courtTypesApi.create(description), onSuccess: async () => { setTypeForm(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['court-types'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const toggle = useMutation({ mutationFn: (item: Court) => item.isActive ? courtsApi.deactivate(item.id) : courtsApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['courts'] }) })
  return <PageShell actionLabel="Nueva cancha" description="Catalogo de canchas, tipos y precio por hora." onCreate={() => setCourtForm({ item: null })} title="Canchas"><Panel>{courtForm ? <CourtForm error={error} initial={courtForm.item} isSaving={courtSave.isPending} onCancel={() => setCourtForm(null)} onSubmit={(payload) => courtSave.mutate(payload)} types={typesQuery.data ?? []} /> : null}<div className="border-b border-slate-200 p-4"><div className="mb-3 flex items-center justify-between"><h4 className="text-sm font-semibold">Tipos de cancha</h4><button className="text-sm font-semibold text-emerald-700" onClick={() => setTypeForm('new')} type="button">Agregar tipo</button></div>{typeForm ? <CourtTypeForm error={error} initial={typeForm === 'new' ? '' : typeForm.description} isSaving={typeSave.isPending} onCancel={() => setTypeForm(null)} onSubmit={(description) => typeSave.mutate(description)} /> : null}<div className="flex flex-wrap gap-2">{(typesQuery.data ?? []).map((type) => <button className="rounded-full border border-slate-200 px-3 py-1 text-sm text-slate-700" key={type.id} onClick={() => setTypeForm(type)} type="button">{type.description}</button>)}</div></div>{courtsQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Cargando...</p> : <CourtsTable items={courtsQuery.data ?? []} onEdit={(item) => setCourtForm({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function CourtTypeForm({ error, initial, isSaving, onCancel, onSubmit }: { error?: string; initial: string; isSaving: boolean; onCancel: () => void; onSubmit: (description: string) => void }) {
  const [description, setDescription] = useState(initial)
  const [validationError, setValidationError] = useState<string>()
  return <form className="mb-3 flex flex-wrap items-start gap-2" onSubmit={(event) => { event.preventDefault(); const parsed = courtTypeSchema.safeParse(description); if (!parsed.success) { setValidationError(parsed.error.issues[0]?.message); return } setValidationError(undefined); onSubmit(parsed.data) }}><FormControl error={validationError} label="Descripción"><input required className="min-w-64 rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setDescription(event.target.value)} value={description} /></FormControl><button className="mt-6 rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button><button className="mt-6 rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button>{error ? <p className="basis-full text-sm text-red-600">{error}</p> : null}</form>
}

function CourtForm({ error, initial, isSaving, onCancel, onSubmit, types }: { error?: string; initial: Court | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: CourtPayload) => void; types: CourtType[] }) {
  const [name, setName] = useState(initial?.name ?? '')
  const [courtTypeId, setCourtTypeId] = useState(initial?.courtTypeId ?? types[0]?.id ?? 0)
  const [hourPrice, setHourPrice] = useState(String(initial?.hourPrice ?? ''))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-3" onSubmit={(event) => { event.preventDefault(); const parsed = courtSchema.safeParse({ name, courtTypeId, hourPrice: hourPrice === '' ? Number.NaN : Number(hourPrice) }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit(parsed.data) }}><h4 className="md:col-span-3 text-sm font-semibold">{initial ? 'Editar cancha' : 'Nueva cancha'}</h4><FormControl error={validationErrors.name} label="Nombre"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setName(event.target.value)} value={name} /></FormControl><FormControl error={validationErrors.courtTypeId} label="Tipo"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setCourtTypeId(Number(event.target.value))} value={courtTypeId || ''}><option value="">Seleccionar tipo</option>{types.map((type) => <option key={type.id} value={type.id}>{type.description}</option>)}</select></FormControl><FormControl error={validationErrors.hourPrice} label="Precio por hora"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" min="0.01" step="0.01" onChange={(event) => setHourPrice(event.target.value)} type="number" value={hourPrice} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-3">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-3"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button></div></form>
}

function CourtsTable({ items, onEdit, onToggle }: { items: Court[]; onEdit: (item: Court) => void; onToggle: (item: Court) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Cancha</th><th className="px-4 py-3">Tipo</th><th className="px-4 py-3">Precio</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.name}</td><td className="px-4 py-3 text-slate-600">{item.courtType}</td><td className="px-4 py-3 text-slate-600">${item.hourPrice}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}

export function TurnsPage() {
  const queryClient = useQueryClient()
  const [formMode, setFormMode] = useState<FormMode<AvailableTurn>>(null)
  const [error, setError] = useState<string>()
  const turnsQuery = useQuery({ queryKey: ['turns'], queryFn: turnsApi.list })
  const courtsQuery = useQuery({ queryKey: ['courts'], queryFn: courtsApi.list })
  const save = useMutation({ mutationFn: (payload: TurnPayload) => formMode?.item ? turnsApi.update(formMode.item.id, payload) : turnsApi.create(payload), onSuccess: async () => { setFormMode(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['turns'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const toggle = useMutation({ mutationFn: (item: AvailableTurn) => item.isActive ? turnsApi.deactivate(item.id) : turnsApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['turns'] }) })
  return <PageShell actionLabel="Nuevo turno" description="Horarios disponibles por cancha para alimentar reservas." onCreate={() => setFormMode({ item: null })} title="Turnos">{formMode ? <Panel><TurnForm courts={courtsQuery.data ?? []} error={error} initial={formMode.item} isSaving={save.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => save.mutate(payload)} /></Panel> : null}<Panel>{turnsQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Cargando...</p> : <TurnsTable items={turnsQuery.data ?? []} onEdit={(item) => setFormMode({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function TurnForm({ courts, error, initial, isSaving, onCancel, onSubmit }: { courts: Court[]; error?: string; initial: AvailableTurn | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: TurnPayload) => void }) {
  const [courtId, setCourtId] = useState(initial?.courtId ?? courts[0]?.id ?? 0)
  const [startTime, setStartTime] = useState((initial?.startTime ?? '09:00').slice(0, 5))
  const [endTime, setEndTime] = useState((initial?.endTime ?? '10:00').slice(0, 5))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 p-4 md:grid-cols-3" onSubmit={(event) => { event.preventDefault(); const parsed = turnSchema.safeParse({ courtId, startTime, endTime }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit({ courtId: parsed.data.courtId, startTime: `${parsed.data.startTime}:00`, endTime: `${parsed.data.endTime}:00` }) }}><FormControl error={validationErrors.courtId} label="Cancha"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setCourtId(Number(event.target.value))} value={courtId || ''}><option value="">Seleccionar cancha</option>{courts.map((court) => <option key={court.id} value={court.id}>{court.name}</option>)}</select></FormControl><FormControl error={validationErrors.startTime} label="Hora de inicio"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setStartTime(event.target.value)} type="time" value={startTime} /></FormControl><FormControl error={validationErrors.endTime} label="Hora de fin"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setEndTime(event.target.value)} type="time" value={endTime} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-3">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-3"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button></div></form>
}

function TurnsTable({ items, onEdit, onToggle }: { items: AvailableTurn[]; onEdit: (item: AvailableTurn) => void; onToggle: (item: AvailableTurn) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Cancha</th><th className="px-4 py-3">Inicio</th><th className="px-4 py-3">Fin</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.courtName}</td><td className="px-4 py-3 text-slate-600">{item.startTime.slice(0, 5)}</td><td className="px-4 py-3 text-slate-600">{item.endTime.slice(0, 5)}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}

export function PromotionsPage() {
  const queryClient = useQueryClient()
  const [formMode, setFormMode] = useState<FormMode<Promotion>>(null)
  const [error, setError] = useState<string>()
  const query = useQuery({ queryKey: ['promotions'], queryFn: promotionsApi.list })
  const save = useMutation({ mutationFn: (payload: PromotionPayload) => formMode?.item ? promotionsApi.update(formMode.item.id, payload) : promotionsApi.create(payload), onSuccess: async () => { setFormMode(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['promotions'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const toggle = useMutation({ mutationFn: (item: Promotion) => item.isActive ? promotionsApi.deactivate(item.id) : promotionsApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['promotions'] }) })
  return <PageShell actionLabel="Nueva promocion" description="Descuentos activos y vigentes para aplicar en reservas." onCreate={() => setFormMode({ item: null })} title="Promociones">{formMode ? <Panel><PromotionForm error={error} initial={formMode.item} isSaving={save.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => save.mutate(payload)} /></Panel> : null}<Panel>{query.isLoading ? <p className="p-4 text-sm text-slate-500">Cargando...</p> : <PromotionsTable items={query.data ?? []} onEdit={(item) => setFormMode({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function PromotionForm({ error, initial, isSaving, onCancel, onSubmit }: { error?: string; initial: Promotion | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: PromotionPayload) => void }) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [discountPercentage, setDiscountPercentage] = useState(String(initial?.discountPercentage ?? ''))
  const [dateFrom, setDateFrom] = useState(initial?.dateFrom ?? new Date().toISOString().slice(0, 10))
  const [dateTo, setDateTo] = useState(initial?.dateTo ?? new Date().toISOString().slice(0, 10))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 p-4 md:grid-cols-2" onSubmit={(event) => { event.preventDefault(); const parsed = promotionSchema.safeParse({ name, description, discountPercentage: discountPercentage === '' ? Number.NaN : Number(discountPercentage), dateFrom, dateTo }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit(parsed.data) }}><FormControl error={validationErrors.name} label="Nombre"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setName(event.target.value)} value={name} /></FormControl><FormControl error={validationErrors.discountPercentage} label="Descuento %"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" max="100" min="0.01" step="0.01" onChange={(event) => setDiscountPercentage(event.target.value)} type="number" value={discountPercentage} /></FormControl><FormControl className="md:col-span-2" error={validationErrors.description} label="Descripción (opcional)"><input className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={255} onChange={(event) => setDescription(event.target.value)} value={description} /></FormControl><FormControl error={validationErrors.dateFrom} label="Desde"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setDateFrom(event.target.value)} type="date" value={dateFrom} /></FormControl><FormControl error={validationErrors.dateTo} label="Hasta"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setDateTo(event.target.value)} type="date" value={dateTo} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancelar</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Guardar</button></div></form>
}

function PromotionsTable({ items, onEdit, onToggle }: { items: Promotion[]; onEdit: (item: Promotion) => void; onToggle: (item: Promotion) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Promocion</th><th className="px-4 py-3">Descuento</th><th className="px-4 py-3">Vigencia</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3"><p className="font-medium">{item.name}</p><p className="text-xs text-slate-500">{item.description ?? '-'}</p></td><td className="px-4 py-3 text-slate-600">{item.discountPercentage}%</td><td className="px-4 py-3 text-slate-600">{item.dateFrom} / {item.dateTo}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}
