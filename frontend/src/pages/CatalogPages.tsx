import { useMemo, useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Eye, KeyRound, Pencil, Plus, Search, X } from 'lucide-react'
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
import type { AvailableTurn, Client, ClientProfile, Court, CourtType, Employee, Promotion, RoleCatalog, UserCatalog } from '@/types/api'

type StatusItem = { id: number; isActive: boolean }
type FormMode<T> = { item: T | null } | null

const emptyPerson: PersonPayload = { firstName: '', lastName: '', dni: '', phone: '', email: '' }
const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function getErrorMessage(error: unknown) {
  return error instanceof ApiRequestError ? error.message : 'The operation could not be completed.'
}

function activeBadge(isActive: boolean) {
  return (
    <span className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold ${isActive ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-slate-100 text-slate-500'}`}>
      {isActive ? 'Active' : 'Inactive'}
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
    <section className="space-y-4 rounded-2xl border border-[#3e4943] bg-[#131b2e]/75 p-5 shadow-[0_18px_46px_rgba(6,14,32,0.22)]">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-2xl font-semibold tracking-normal text-[#dae2fd]">{title}</h3>
          <p className="mt-1 max-w-2xl text-sm leading-6 text-[#bdc9c1]">{description}</p>
        </div>
        <button className="inline-flex translate-y-0 items-center gap-2 rounded-xl bg-[#057a55] px-4 py-2.5 text-sm font-semibold text-white shadow-[0_12px_24px_rgba(0,82,54,0.24)] transition duration-200 hover:-translate-y-px hover:bg-[#006c4b] active:translate-y-0" onClick={onCreate} type="button">
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
    <label className="flex w-full max-w-sm items-center gap-2 rounded-xl border border-[#3e4943] bg-[#131b2e] px-3 py-2.5 text-sm text-[#bdc9c1] shadow-[0_8px_20px_rgba(6,14,32,0.16)] transition focus-within:border-[#7ad9ad] focus-within:ring-4 focus-within:ring-[#7ad9ad]/10">
      <Search aria-hidden="true" className="size-4 shrink-0 text-[#88948b]" strokeWidth={1.9} />
      <input className="w-full bg-transparent text-sm text-[#dae2fd] outline-none placeholder:text-[#88948b]" onChange={(event) => onChange(event.target.value)} placeholder="Search" value={value} />
    </label>
  )
}

function Panel({ children }: { children: ReactNode }) {
  return <div className="overflow-hidden rounded-2xl border border-[#3e4943] bg-[#171f33] shadow-[0_14px_35px_rgba(6,14,32,0.2)]">{children}</div>
}

function FormControl({ children, error, label, className = '' }: { children: ReactNode; error?: string; label: string; className?: string }) {
  return <label className={`grid gap-1 text-sm font-medium text-[#bdc9c1] ${className}`}><span>{label}</span>{children}{error ? <span className="text-xs font-medium text-[#ffb4ab]" role="alert">{error}</span> : null}</label>
}

function ActionButton({ children, onClick, title }: { children: React.ReactNode; onClick: () => void; title: string }) {
  return (
    <button className="inline-flex size-8 items-center justify-center rounded-lg border border-[#3e4943] bg-[#222a3d] text-[#bdc9c1] shadow-[0_6px_14px_rgba(6,14,32,0.16)] transition duration-200 hover:-translate-y-px hover:border-[#7ad9ad]/40 hover:bg-[#2d3449] hover:text-[#7ad9ad] active:translate-y-0" onClick={onClick} title={title} type="button">
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
      <ActionButton onClick={() => onEdit(item)} title="Edit">
        <Pencil aria-hidden="true" className="size-4" />
      </ActionButton>
      <ActionButton onClick={() => onToggle(item)} title={item.isActive ? 'Deactivate' : 'Activate'}>
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
      <FormControl error={fieldErrors.firstName} label="First name"><input aria-invalid={Boolean(fieldErrors.firstName)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={60} onChange={(event) => update('firstName', event.target.value)} value={form.firstName} /></FormControl>
      <FormControl error={fieldErrors.lastName} label="Last name"><input aria-invalid={Boolean(fieldErrors.lastName)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={60} onChange={(event) => update('lastName', event.target.value)} value={form.lastName} /></FormControl>
      <FormControl error={fieldErrors.dni} label="Customer ID"><input aria-invalid={Boolean(fieldErrors.dni)} inputMode="numeric" required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={10} onChange={(event) => update('dni', event.target.value)} value={form.dni} /></FormControl>
      <FormControl error={fieldErrors.phone} label="Phone"><input aria-invalid={Boolean(fieldErrors.phone)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={40} onChange={(event) => update('phone', event.target.value)} type="tel" value={form.phone} /></FormControl>
      <FormControl className="md:col-span-2" error={fieldErrors.email} label="Email"><input aria-invalid={Boolean(fieldErrors.email)} required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={120} onChange={(event) => update('email', event.target.value)} type="email" value={form.email} /></FormControl>
      {error ? <p className="text-sm text-red-600 md:col-span-2" role="alert">{error}</p> : null}
      <div className="flex justify-end gap-2 md:col-span-2">
        <button className="rounded-md border border-slate-200 px-3 py-2 text-sm font-medium" onClick={onCancel} type="button">Cancel</button>
        <button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white disabled:opacity-60" disabled={isSaving} type="submit">{isSaving ? 'Saving...' : 'Save'}</button>
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
  onProfile,
  onToggle,
}: {
  items: TItem[]
  kind: 'clients' | 'employees'
  onEdit: (item: TItem) => void
  onProfile?: (item: TItem) => void
  onToggle: (item: TItem) => void
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="bg-[#F8FAFC] text-xs uppercase text-[#334155]">
          <tr>
            <th className="px-4 py-3 font-bold">Person</th>
            <th className="px-4 py-3 font-bold">Customer ID</th>
            <th className="px-4 py-3 font-bold">Contact</th>
            {kind === 'employees' ? <th className="px-4 py-3 font-bold">User</th> : null}
            <th className="px-4 py-3 font-bold">Status</th>
            <th className="px-4 py-3 text-right font-bold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-[#E2E8F0]">
          {items.map((item) => (
            <tr className="transition hover:bg-[#F8FAFC]" key={item.id}>
              <td className="px-4 py-3 font-medium text-[#0F172A]">{item.firstName} {item.lastName}</td>
              <td className="px-4 py-3 text-[#334155]">{item.dni || '-'}</td>
              <td className="px-4 py-3 text-[#334155]">{item.phone || item.email || '-'}</td>
              {kind === 'employees' ? <td className="px-4 py-3 text-[#334155]">{(item as Employee).hasUser ? 'Assigned' : 'No user'}</td> : null}
              <td className="px-4 py-3">{activeBadge(item.isActive)}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  {onProfile ? (
                    <ActionButton onClick={() => onProfile(item)} title="View profile">
                      <Eye aria-hidden="true" className="size-4" />
                    </ActionButton>
                  ) : null}
                  <StatusActions item={item} onEdit={onEdit} onToggle={onToggle} />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function formatDate(value?: string | null) {
  return value ? new Date(`${value}T00:00:00`).toLocaleDateString('en-US') : '-'
}

function ClientProfilePanel({
  isError,
  isLoading,
  onClose,
  profile,
  refetch,
}: {
  isError: boolean
  isLoading: boolean
  onClose: () => void
  profile?: ClientProfile
  refetch: () => void
}) {
  const favoriteSlot = profile?.favoriteDayName && profile.favoriteStartTime
    ? `${profile.favoriteDayName} ${profile.favoriteStartTime.slice(0, 5)}`
    : '-'
  const hasProfileData = Boolean(profile && profile.totalReservations > 0)

  return (
    <aside className="border-t border-[#E2E8F0] bg-[#F8FAFC] p-4">
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h4 className="text-base font-bold text-[#0F172A]">{profile?.clientName ?? 'Customer profile'}</h4>
          <p className="mt-1 text-sm text-[#475569]">{profile ? `${profile.phone || '-'} - ${profile.email || '-'}` : 'Loading customer metrics...'}</p>
        </div>
        <button className="rounded-lg border border-[#CBD5E1] px-3 py-2 text-sm font-bold text-[#334155] transition hover:bg-white" onClick={onClose} type="button">Close</button>
      </div>
      {isLoading ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4].map((item) => <div className="h-24 animate-pulse rounded-xl bg-[#E2E8F0]" key={item} />)}
        </div>
      ) : isError ? (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-red-200 bg-red-50 p-4" role="alert">
          <p className="text-sm font-semibold text-red-700">The customer profile could not be loaded.</p>
          <button className="rounded-lg border border-red-300 px-3 py-2 text-sm font-bold text-red-700" onClick={refetch} type="button">Retry</button>
        </div>
      ) : profile && !hasProfileData ? (
        <div className="rounded-xl border border-[#CBD5E1] bg-white p-5">
          <p className="text-sm font-bold text-[#0F172A]">No activity has been recorded for this customer.</p>
          <p className="mt-1 text-sm text-[#64748B]">Once reservations or payments are recorded, this profile will show activity and lifetime value.</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-3">
            <ProfileMetric label="Customer ID" value={profile.dni || '-'} />
            <ProfileMetric label="Contact" value={profile.phone || profile.email || '-'} />
            <ProfileMetric label="Status" value={profile.isActive ? 'Active' : 'Inactive'} />
          </div>
        </div>
      ) : profile ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <ProfileMetric label="Reservations" value={String(profile.totalReservations)} />
          <ProfileMetric label="Paid" value={money.format(profile.totalPaid)} />
          <ProfileMetric label="Outstanding balance" value={money.format(profile.pendingBalance)} />
          <ProfileMetric label="Cancellations" value={String(profile.cancellationCount)} />
          <ProfileMetric label="Favorite time" value={favoriteSlot} />
          <ProfileMetric label="Last visit" value={formatDate(profile.lastVisitDate)} />
          <ProfileMetric label="Customer ID" value={profile.dni || '-'} />
          <ProfileMetric label="Status" value={profile.isActive ? 'Active' : 'Inactive'} />
        </div>
      ) : null}
    </aside>
  )
}

function ProfileMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-[#E2E8F0] bg-white p-4 shadow-[0_8px_20px_rgba(15,23,42,0.06)]">
      <p className="text-xs font-bold uppercase text-[#64748B]">{label}</p>
      <p className="mt-2 break-words text-lg font-black text-[#0F172A]">{value}</p>
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
    profile?: (id: number) => Promise<ClientProfile>
  }
  description: string
  kind: 'clients' | 'employees'
  queryKey: string
  title: string
}) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [formMode, setFormMode] = useState<FormMode<TItem>>(null)
  const [selectedProfileId, setSelectedProfileId] = useState<number | null>(null)
  const [error, setError] = useState<string>()
  const [successMessage, setSuccessMessage] = useState<string>()
  const query = useQuery({ queryKey: [queryKey], queryFn: api.list })
  const profileQuery = useQuery({
    enabled: kind === 'clients' && selectedProfileId !== null && Boolean(api.profile),
    queryKey: ['client-profile', selectedProfileId],
    queryFn: () => api.profile?.(selectedProfileId ?? 0) ?? Promise.reject(new Error('Customer profile is unavailable.')),
  })
  const saveMutation = useMutation({
    mutationFn: (payload: PersonPayload) => formMode?.item ? api.update(formMode.item.id, payload) : api.create(payload),
    onMutate: () => {
      setError(undefined)
      setSuccessMessage(undefined)
    },
    onSuccess: (savedItem) => {
      const wasEditing = Boolean(formMode?.item)

      try {
        queryClient.setQueryData<TItem[]>([queryKey], (currentItems = []) => {
          const itemIndex = currentItems.findIndex((item) => item.id === savedItem.id)
          const nextItems = itemIndex >= 0
            ? currentItems.map((item) => item.id === savedItem.id ? savedItem : item)
            : [...currentItems, savedItem]

          return nextItems.toSorted((left, right) =>
            `${left.lastName} ${left.firstName}`.localeCompare(
              `${right.lastName} ${right.firstName}`,
              'en-US',
              { sensitivity: 'base' },
            ))
        })

      } catch {
        setError('The record was saved, but the list could not be refreshed. Reload the page to verify it.')
        return
      }

      if (!wasEditing) setSearch('')
      setFormMode(null)
      setError(undefined)
      setSuccessMessage(
        kind === 'employees'
          ? `Staff member ${wasEditing ? 'updated' : 'created'} successfully.`
          : `Customer ${wasEditing ? 'updated' : 'created'} successfully.`,
      )
      void queryClient.invalidateQueries({ queryKey: [queryKey] })
    },
    onError: (mutationError) => setError(getErrorMessage(mutationError)),
  })
  const toggleMutation = useMutation({
    mutationFn: (item: TItem) => item.isActive ? api.deactivate(item.id) : api.activate(item.id),
    onSuccess: (_data, item) => {
      void queryClient.invalidateQueries({ queryKey: [queryKey] })
      if (kind === 'clients') void queryClient.invalidateQueries({ queryKey: ['client-profile', item.id] })
    },
  })
  const items = useMemo(() => (query.data ?? []).filter((item) => `${item.firstName} ${item.lastName} ${item.dni ?? ''}`.toLowerCase().includes(search.toLowerCase())), [query.data, search])

  return (
    <PageShell
      actionLabel={`New ${kind === 'clients' ? 'customer' : 'staff'}`}
      description={description}
      onCreate={() => {
        setError(undefined)
        setSuccessMessage(undefined)
        setSelectedProfileId(null)
        setFormMode({ item: null })
      }}
      title={title}
    >
      {successMessage ? <p aria-live="polite" className="rounded-xl border border-emerald-700/40 bg-emerald-950/40 px-4 py-3 text-sm font-medium text-emerald-200" role="status">{successMessage}</p> : null}
      <SearchBox onChange={setSearch} value={search} />
      <Panel>
        {formMode ? <PersonForm error={error} initial={toPersonPayload(formMode.item)} isSaving={saveMutation.isPending} onCancel={() => { setFormMode(null); setError(undefined) }} onSubmit={(payload) => saveMutation.mutate(payload)} title={formMode.item ? 'Edit record' : 'New record'} /> : null}
        {query.isError && query.data ? (
          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-amber-700/30 bg-amber-950/30 p-4" role="alert">
            <p className="text-sm text-amber-200">The list could not be refreshed. Showing the latest available data.</p>
            <button className="rounded-md border border-amber-300/60 px-3 py-2 text-sm font-medium text-amber-100" onClick={() => query.refetch()} type="button">Retry</button>
          </div>
        ) : null}
        {query.isLoading ? (
          <p className="p-4 text-sm text-slate-500">Loading...</p>
        ) : query.isError && !query.data ? (
          <div className="flex flex-wrap items-center justify-between gap-3 p-4" role="alert">
            <p className="text-sm text-red-600">The list could not be loaded. Try again.</p>
            <button className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700" onClick={() => query.refetch()} type="button">Retry</button>
          </div>
        ) : (
          <PeopleTable
            items={items}
            kind={kind}
            onEdit={(item) => {
              setError(undefined)
              setSuccessMessage(undefined)
              setSelectedProfileId(null)
              setFormMode({ item })
            }}
            onProfile={kind === 'clients' ? (item) => {
              setFormMode(null)
              setError(undefined)
              setSuccessMessage(undefined)
              setSelectedProfileId((currentId) => currentId === item.id ? null : item.id)
            } : undefined}
            onToggle={(item) => toggleMutation.mutate(item)}
          />
        )}
        {kind === 'clients' && selectedProfileId !== null ? (
          <ClientProfilePanel
            isError={profileQuery.isError}
            isLoading={profileQuery.isLoading}
            onClose={() => setSelectedProfileId(null)}
            profile={profileQuery.data}
            refetch={() => { void profileQuery.refetch() }}
          />
        ) : null}
      </Panel>
    </PageShell>
  )
}

export function ClientsPage() {
  return <PeoplePage api={clientsApi} description="Manage player profiles and contact details for future reservations." kind="clients" queryKey="clients" title="Customers" />
}

export function EmployeesPage() {
  return <PeoplePage api={employeesApi} description="Manage club staff profiles and contact information." kind="employees" queryKey="employees" title="Staff" />
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
    <PageShell actionLabel="New user" description="Manage internal user accounts, roles, and access status." onCreate={() => setFormMode({ item: null })} title="Users">
      <Panel>
        {formMode ? <UserForm employees={employeesQuery.data ?? []} error={error} initial={formMode.item} isSaving={saveMutation.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => saveMutation.mutate(payload)} roles={rolesQuery.data ?? []} /> : null}
        {passwordUser ? <PasswordForm error={error} isSaving={saveMutation.isPending} onCancel={() => setPasswordUser(null)} onSubmit={(password) => usersApi.changePassword(passwordUser.id, password).then(async () => { setPasswordUser(null); await queryClient.invalidateQueries({ queryKey: ['users'] }) }).catch((mutationError: unknown) => setError(getErrorMessage(mutationError)))} username={passwordUser.username} /> : null}
        {usersQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Loading...</p> : <UsersTable items={usersQuery.data ?? []} onEdit={(item) => setFormMode({ item })} onPassword={setPasswordUser} onToggle={(item) => toggleMutation.mutate(item)} />}
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
      <h4 className="md:col-span-2 text-sm font-semibold">{initial ? 'Edit user' : 'New user'}</h4>
      <FormControl error={validationErrors.username} label="Username"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={50} onChange={(event) => setUsername(event.target.value)} value={username} /></FormControl>
      {!initial ? <FormControl error={validationErrors.password} label="Password"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" minLength={8} maxLength={100} onChange={(event) => setPassword(event.target.value)} type="password" value={password} /></FormControl> : null}
      {!initial ? <FormControl error={validationErrors.employeeId} label="Staff"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setEmployeeId(Number(event.target.value))} value={employeeId || ''}><option value="">Select staff</option>{availableEmployees.map((employee) => <option key={employee.id} value={employee.id}>{employee.firstName} {employee.lastName}</option>)}</select></FormControl> : <p className="rounded-md border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-600">{initial.employeeName}</p>}
      <FormControl error={validationErrors.roleId} label="Role"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setRoleId(Number(event.target.value))} value={roleId || ''}><option value="">Select role</option>{roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}</select></FormControl>
      {error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}
      <div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button></div>
    </form>
  )
}

function PasswordForm({ error, isSaving, onCancel, onSubmit, username }: { error?: string; isSaving: boolean; onCancel: () => void; onSubmit: (password: string) => void; username: string }) {
  const [password, setPassword] = useState('')
  const [validationError, setValidationError] = useState<string>()
  return <form className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-2" onSubmit={(event) => { event.preventDefault(); const parsed = passwordSchema.safeParse(password); if (!parsed.success) { setValidationError(parsed.error.issues[0]?.message); return } setValidationError(undefined); onSubmit(parsed.data) }}><h4 className="md:col-span-2 text-sm font-semibold">Change password for {username}</h4><FormControl error={validationError} label="New password"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" minLength={8} maxLength={100} onChange={(event) => setPassword(event.target.value)} type="password" value={password} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button></div></form>
}

function UsersTable({ items, onEdit, onPassword, onToggle }: { items: UserCatalog[]; onEdit: (item: UserCatalog) => void; onPassword: (item: UserCatalog) => void; onToggle: (item: UserCatalog) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">User</th><th className="px-4 py-3">Staff</th><th className="px-4 py-3">Role</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.username}</td><td className="px-4 py-3 text-slate-600">{item.employeeName}</td><td className="px-4 py-3 text-slate-600">{item.role}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><div className="flex justify-end gap-2"><ActionButton onClick={() => onEdit(item)} title="Edit"><Pencil aria-hidden="true" className="size-4" /></ActionButton><ActionButton onClick={() => onPassword(item)} title="Change password"><KeyRound aria-hidden="true" className="size-4" /></ActionButton><ActionButton onClick={() => onToggle(item)} title={item.isActive ? 'Deactivate' : 'Activate'}>{item.isActive ? <X aria-hidden="true" className="size-4" /> : <Check aria-hidden="true" className="size-4" />}</ActionButton></div></td></tr>)}</tbody></table></div>
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
  return <PageShell actionLabel="New court" description="Court catalog, surface types, and hourly pricing." onCreate={() => setCourtForm({ item: null })} title="Courts"><Panel>{courtForm ? <CourtForm error={error} initial={courtForm.item} isSaving={courtSave.isPending} onCancel={() => setCourtForm(null)} onSubmit={(payload) => courtSave.mutate(payload)} types={typesQuery.data ?? []} /> : null}<div className="border-b border-slate-200 p-4"><div className="mb-3 flex items-center justify-between"><h4 className="text-sm font-semibold">Court types</h4><button className="text-sm font-semibold text-emerald-700" onClick={() => setTypeForm('new')} type="button">Add type</button></div>{typeForm ? <CourtTypeForm error={error} initial={typeForm === 'new' ? '' : typeForm.description} isSaving={typeSave.isPending} onCancel={() => setTypeForm(null)} onSubmit={(description) => typeSave.mutate(description)} /> : null}<div className="flex flex-wrap gap-2">{(typesQuery.data ?? []).map((type) => <button className="rounded-full border border-slate-200 px-3 py-1 text-sm text-slate-700" key={type.id} onClick={() => setTypeForm(type)} type="button">{type.description}</button>)}</div></div>{courtsQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Loading...</p> : <CourtsTable items={courtsQuery.data ?? []} onEdit={(item) => setCourtForm({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function CourtTypeForm({ error, initial, isSaving, onCancel, onSubmit }: { error?: string; initial: string; isSaving: boolean; onCancel: () => void; onSubmit: (description: string) => void }) {
  const [description, setDescription] = useState(initial)
  const [validationError, setValidationError] = useState<string>()
  return <form className="mb-3 flex flex-wrap items-start gap-2" onSubmit={(event) => { event.preventDefault(); const parsed = courtTypeSchema.safeParse(description); if (!parsed.success) { setValidationError(parsed.error.issues[0]?.message); return } setValidationError(undefined); onSubmit(parsed.data) }}><FormControl error={validationError} label="Description"><input required className="min-w-64 rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setDescription(event.target.value)} value={description} /></FormControl><button className="mt-6 rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button><button className="mt-6 rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button>{error ? <p className="basis-full text-sm text-red-600">{error}</p> : null}</form>
}

function CourtForm({ error, initial, isSaving, onCancel, onSubmit, types }: { error?: string; initial: Court | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: CourtPayload) => void; types: CourtType[] }) {
  const [name, setName] = useState(initial?.name ?? '')
  const [courtTypeId, setCourtTypeId] = useState(initial?.courtTypeId ?? types[0]?.id ?? 0)
  const [hourPrice, setHourPrice] = useState(String(initial?.hourPrice ?? ''))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-3" onSubmit={(event) => { event.preventDefault(); const parsed = courtSchema.safeParse({ name, courtTypeId, hourPrice: hourPrice === '' ? Number.NaN : Number(hourPrice) }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit(parsed.data) }}><h4 className="md:col-span-3 text-sm font-semibold">{initial ? 'Edit court' : 'New court'}</h4><FormControl error={validationErrors.name} label="Court name"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setName(event.target.value)} value={name} /></FormControl><FormControl error={validationErrors.courtTypeId} label="Type"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setCourtTypeId(Number(event.target.value))} value={courtTypeId || ''}><option value="">Select type</option>{types.map((type) => <option key={type.id} value={type.id}>{type.description}</option>)}</select></FormControl><FormControl error={validationErrors.hourPrice} label="Hourly price"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" min="0.01" step="0.01" onChange={(event) => setHourPrice(event.target.value)} type="number" value={hourPrice} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-3">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-3"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button></div></form>
}

function CourtsTable({ items, onEdit, onToggle }: { items: Court[]; onEdit: (item: Court) => void; onToggle: (item: Court) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Court</th><th className="px-4 py-3">Type</th><th className="px-4 py-3">Price</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.name}</td><td className="px-4 py-3 text-slate-600">{item.courtType}</td><td className="px-4 py-3 text-slate-600">${item.hourPrice}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}

export function TurnsPage() {
  const queryClient = useQueryClient()
  const [formMode, setFormMode] = useState<FormMode<AvailableTurn>>(null)
  const [error, setError] = useState<string>()
  const turnsQuery = useQuery({ queryKey: ['turns'], queryFn: turnsApi.list })
  const courtsQuery = useQuery({ queryKey: ['courts'], queryFn: courtsApi.list })
  const save = useMutation({ mutationFn: (payload: TurnPayload) => formMode?.item ? turnsApi.update(formMode.item.id, payload) : turnsApi.create(payload), onSuccess: async () => { setFormMode(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['turns'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const toggle = useMutation({ mutationFn: (item: AvailableTurn) => item.isActive ? turnsApi.deactivate(item.id) : turnsApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['turns'] }) })
  return <PageShell actionLabel="New time slot" description="Available court time slots used for reservations." onCreate={() => setFormMode({ item: null })} title="Time slots">{formMode ? <Panel><TurnForm courts={courtsQuery.data ?? []} error={error} initial={formMode.item} isSaving={save.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => save.mutate(payload)} /></Panel> : null}<Panel>{turnsQuery.isLoading ? <p className="p-4 text-sm text-slate-500">Loading...</p> : <TurnsTable items={turnsQuery.data ?? []} onEdit={(item) => setFormMode({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function TurnForm({ courts, error, initial, isSaving, onCancel, onSubmit }: { courts: Court[]; error?: string; initial: AvailableTurn | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: TurnPayload) => void }) {
  const [courtId, setCourtId] = useState(initial?.courtId ?? courts[0]?.id ?? 0)
  const [startTime, setStartTime] = useState((initial?.startTime ?? '09:00').slice(0, 5))
  const [endTime, setEndTime] = useState((initial?.endTime ?? '10:00').slice(0, 5))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 p-4 md:grid-cols-3" onSubmit={(event) => { event.preventDefault(); const parsed = turnSchema.safeParse({ courtId, startTime, endTime }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit({ courtId: parsed.data.courtId, startTime: `${parsed.data.startTime}:00`, endTime: `${parsed.data.endTime}:00` }) }}><FormControl error={validationErrors.courtId} label="Court"><select required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setCourtId(Number(event.target.value))} value={courtId || ''}><option value="">Select court</option>{courts.map((court) => <option key={court.id} value={court.id}>{court.name}</option>)}</select></FormControl><FormControl error={validationErrors.startTime} label="Start time"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setStartTime(event.target.value)} type="time" value={startTime} /></FormControl><FormControl error={validationErrors.endTime} label="End time"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setEndTime(event.target.value)} type="time" value={endTime} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-3">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-3"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button></div></form>
}

function TurnsTable({ items, onEdit, onToggle }: { items: AvailableTurn[]; onEdit: (item: AvailableTurn) => void; onToggle: (item: AvailableTurn) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Court</th><th className="px-4 py-3">Start</th><th className="px-4 py-3">End</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3 font-medium">{item.courtName}</td><td className="px-4 py-3 text-slate-600">{item.startTime.slice(0, 5)}</td><td className="px-4 py-3 text-slate-600">{item.endTime.slice(0, 5)}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}

export function PromotionsPage() {
  const queryClient = useQueryClient()
  const [formMode, setFormMode] = useState<FormMode<Promotion>>(null)
  const [error, setError] = useState<string>()
  const query = useQuery({ queryKey: ['promotions'], queryFn: promotionsApi.list })
  const save = useMutation({ mutationFn: (payload: PromotionPayload) => formMode?.item ? promotionsApi.update(formMode.item.id, payload) : promotionsApi.create(payload), onSuccess: async () => { setFormMode(null); setError(undefined); await queryClient.invalidateQueries({ queryKey: ['promotions'] }) }, onError: (mutationError) => setError(getErrorMessage(mutationError)) })
  const toggle = useMutation({ mutationFn: (item: Promotion) => item.isActive ? promotionsApi.deactivate(item.id) : promotionsApi.activate(item.id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['promotions'] }) })
  return <PageShell actionLabel="New promotion" description="Active discounts that can be applied to reservations." onCreate={() => setFormMode({ item: null })} title="Promotions">{formMode ? <Panel><PromotionForm error={error} initial={formMode.item} isSaving={save.isPending} onCancel={() => setFormMode(null)} onSubmit={(payload) => save.mutate(payload)} /></Panel> : null}<Panel>{query.isLoading ? <p className="p-4 text-sm text-slate-500">Loading...</p> : <PromotionsTable items={query.data ?? []} onEdit={(item) => setFormMode({ item })} onToggle={(item) => toggle.mutate(item)} />}</Panel></PageShell>
}

function PromotionForm({ error, initial, isSaving, onCancel, onSubmit }: { error?: string; initial: Promotion | null; isSaving: boolean; onCancel: () => void; onSubmit: (payload: PromotionPayload) => void }) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [discountPercentage, setDiscountPercentage] = useState(String(initial?.discountPercentage ?? ''))
  const [dateFrom, setDateFrom] = useState(initial?.dateFrom ?? new Date().toISOString().slice(0, 10))
  const [dateTo, setDateTo] = useState(initial?.dateTo ?? new Date().toISOString().slice(0, 10))
  const [validationErrors, setValidationErrors] = useState<FieldErrors>({})
  return <form className="grid gap-3 p-4 md:grid-cols-2" onSubmit={(event) => { event.preventDefault(); const parsed = promotionSchema.safeParse({ name, description, discountPercentage: discountPercentage === '' ? Number.NaN : Number(discountPercentage), dateFrom, dateTo }); if (!parsed.success) { setValidationErrors(toFieldErrors(parsed.error)); return } setValidationErrors({}); onSubmit(parsed.data) }}><FormControl error={validationErrors.name} label="Promotion name"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={80} onChange={(event) => setName(event.target.value)} value={name} /></FormControl><FormControl error={validationErrors.discountPercentage} label="Discount %"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" max="100" min="0.01" step="0.01" onChange={(event) => setDiscountPercentage(event.target.value)} type="number" value={discountPercentage} /></FormControl><FormControl className="md:col-span-2" error={validationErrors.description} label="Description (optional)"><input className="rounded-md border border-slate-200 px-3 py-2 text-sm" maxLength={255} onChange={(event) => setDescription(event.target.value)} value={description} /></FormControl><FormControl error={validationErrors.dateFrom} label="From"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setDateFrom(event.target.value)} type="date" value={dateFrom} /></FormControl><FormControl error={validationErrors.dateTo} label="To"><input required className="rounded-md border border-slate-200 px-3 py-2 text-sm" onChange={(event) => setDateTo(event.target.value)} type="date" value={dateTo} /></FormControl>{error ? <p className="text-sm text-red-600 md:col-span-2">{error}</p> : null}<div className="flex justify-end gap-2 md:col-span-2"><button className="rounded-md border border-slate-200 px-3 py-2 text-sm" onClick={onCancel} type="button">Cancel</button><button className="rounded-md bg-emerald-700 px-3 py-2 text-sm font-semibold text-white" disabled={isSaving} type="submit">Save</button></div></form>
}

function PromotionsTable({ items, onEdit, onToggle }: { items: Promotion[]; onEdit: (item: Promotion) => void; onToggle: (item: Promotion) => void }) {
  return <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr><th className="px-4 py-3">Promotion</th><th className="px-4 py-3">Discount</th><th className="px-4 py-3">Valid dates</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-100">{items.map((item) => <tr key={item.id}><td className="px-4 py-3"><p className="font-medium">{item.name}</p><p className="text-xs text-slate-500">{item.description ?? '-'}</p></td><td className="px-4 py-3 text-slate-600">{item.discountPercentage}%</td><td className="px-4 py-3 text-slate-600">{item.dateFrom} / {item.dateTo}</td><td className="px-4 py-3">{activeBadge(item.isActive)}</td><td className="px-4 py-3"><StatusActions item={item} onEdit={onEdit} onToggle={onToggle} /></td></tr>)}</tbody></table></div>
}
