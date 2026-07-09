type PlaceholderPageProps = {
  title: string
  description: string
}

export function PlaceholderPage({ description, title }: PlaceholderPageProps) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5">
      <p className="text-sm font-medium text-emerald-700">Modulo protegido</p>
      <h3 className="mt-2 text-2xl font-semibold">{title}</h3>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">{description}</p>
    </section>
  )
}
