const modules = [
  'Dashboard',
  'Clientes',
  'Empleados',
  'Usuarios',
  'Canchas',
  'Turnos',
  'Promociones',
  'Reservas',
  'Pagos',
  'Reportes',
]

function App() {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-950">
      <div className="mx-auto flex min-h-screen max-w-7xl">
        <aside className="hidden w-64 border-r border-slate-200 bg-white px-4 py-5 lg:block">
          <div className="mb-8">
            <p className="text-sm font-medium text-emerald-700">Padelito v2</p>
            <h1 className="mt-1 text-xl font-semibold">Backoffice</h1>
          </div>
          <nav className="space-y-1">
            {modules.map((module) => (
              <button
                className="flex w-full items-center rounded-md px-3 py-2 text-left text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-950"
                key={module}
                type="button"
              >
                {module}
              </button>
            ))}
          </nav>
        </aside>

        <section className="flex min-w-0 flex-1 flex-col">
          <header className="flex items-center justify-between border-b border-slate-200 bg-white px-5 py-4">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                Etapa 1
              </p>
              <h2 className="text-lg font-semibold">Fundaciones listas para conectar API</h2>
            </div>
            <span className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-700">
              .NET 10 + React
            </span>
          </header>

          <div className="grid gap-4 p-5 md:grid-cols-3">
            <article className="rounded-lg border border-slate-200 bg-white p-4">
              <p className="text-sm font-medium text-slate-500">Backend</p>
              <p className="mt-2 text-2xl font-semibold">ASP.NET Core API</p>
              <p className="mt-2 text-sm text-slate-600">
                Controllers, JWT, CORS, Swagger y EF Core configurados para la v2.
              </p>
            </article>
            <article className="rounded-lg border border-slate-200 bg-white p-4">
              <p className="text-sm font-medium text-slate-500">Base</p>
              <p className="mt-2 text-2xl font-semibold">PADELITO_V2_DB</p>
              <p className="mt-2 text-sm text-slate-600">
                Migracion inicial con entidades, constraints y seed minimo.
              </p>
            </article>
            <article className="rounded-lg border border-slate-200 bg-white p-4">
              <p className="text-sm font-medium text-slate-500">Frontend</p>
              <p className="mt-2 text-2xl font-semibold">React + TypeScript</p>
              <p className="mt-2 text-sm text-slate-600">
                Vite y Tailwind listos para construir login y layout privado.
              </p>
            </article>
          </div>
        </section>
      </div>
    </main>
  )
}

export default App
