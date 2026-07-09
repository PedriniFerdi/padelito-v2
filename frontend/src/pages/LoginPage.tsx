import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { ApiRequestError } from '@/api/http'
import { useAuth } from '@/features/auth/AuthProvider'

const loginSchema = z.object({
  username: z.string().trim().min(1, 'Ingresá tu usuario.'),
  password: z.string().min(1, 'Ingresá tu contraseña.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

type LocationState = {
  from?: {
    pathname?: string
  }
}

export function LoginPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<LoginFormValues>({
    defaultValues: {
      username: 'admin',
      password: '',
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    const parsed = loginSchema.safeParse(values)
    if (!parsed.success) {
      for (const issue of parsed.error.issues) {
        const field = issue.path[0]
        if (field === 'username' || field === 'password') {
          setError(field, { message: issue.message })
        }
      }
      return
    }

    try {
      await auth.login(parsed.data)
      const state = location.state as LocationState | null
      navigate(state?.from?.pathname ?? '/dashboard', { replace: true })
    } catch (error) {
      if (error instanceof ApiRequestError && error.statusCode === 401) {
        setFormError('Usuario o contraseña incorrectos.')
        return
      }

      setFormError('No se pudo iniciar sesion. Revisá que la API esté corriendo.')
    }
  })

  return (
    <main className="grid min-h-screen bg-slate-950 px-4 py-8 text-slate-950 md:grid-cols-[1fr_440px]">
      <section className="hidden min-h-full flex-col justify-end rounded-lg bg-[linear-gradient(135deg,#064e3b,#0f766e_45%,#1e293b)] p-10 text-white md:flex">
        <p className="text-sm font-medium text-emerald-100">Padelito v2</p>
        <h1 className="mt-3 max-w-xl text-4xl font-semibold">Backoffice interno para operar el club.</h1>
        <p className="mt-4 max-w-lg text-base text-emerald-50">
          Seguridad JWT, roles y navegación privada listos para construir reservas, pagos y reportes.
        </p>
      </section>

      <section className="flex items-center justify-center md:px-8">
        <form
          className="w-full max-w-md rounded-lg border border-slate-200 bg-white p-6 shadow-xl"
          onSubmit={onSubmit}
        >
          <div>
            <p className="text-sm font-medium text-emerald-700">Padelito v2</p>
            <h2 className="mt-2 text-2xl font-semibold">Iniciar sesion</h2>
            <p className="mt-2 text-sm text-slate-500">Acceso para staff del club.</p>
          </div>

          <div className="mt-6 space-y-4">
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Usuario</span>
              <input
                autoComplete="username"
                className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
                {...register('username')}
              />
              {errors.username?.message ? (
                <span className="mt-1 block text-sm text-red-600">{errors.username.message}</span>
              ) : null}
            </label>

            <label className="block">
              <span className="text-sm font-medium text-slate-700">Contraseña</span>
              <input
                autoComplete="current-password"
                className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
                type="password"
                {...register('password')}
              />
              {errors.password?.message ? (
                <span className="mt-1 block text-sm text-red-600">{errors.password.message}</span>
              ) : null}
            </label>
          </div>

          {formError ? (
            <div className="mt-5 rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {formError}
            </div>
          ) : null}

          <button
            className="mt-6 h-11 w-full rounded-md bg-emerald-700 px-4 text-sm font-semibold text-white hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={isSubmitting}
            type="submit"
          >
            {isSubmitting ? 'Ingresando...' : 'Ingresar'}
          </button>
        </form>
      </section>
    </main>
  )
}
