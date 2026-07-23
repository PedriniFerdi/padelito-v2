import { useState } from 'react'
import { ArrowRight, LoaderCircle, LockKeyhole, UserRound } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { ApiRequestError } from '@/api/http'
import padelBallHero from '@/assets/padel-ball-hero.png'
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

      setFormError('No se pudo iniciar sesión. Revisá que la API esté corriendo.')
    }
  })

  return (
    <main className="relative min-h-[100dvh] overflow-hidden bg-[#080a0b] px-4 py-8 text-[#dce3ed] sm:px-6 lg:py-10">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_31%_55%,rgba(19,56,47,0.16),transparent_31%),radial-gradient(circle_at_50%_50%,rgba(255,255,255,0.018),transparent_54%)]"
      />

      <div className="relative mx-auto grid min-h-[calc(100dvh-5rem)] w-full max-w-[1500px] origin-center scale-90 items-center lg:grid-cols-[minmax(0,1fr)_minmax(480px,600px)] lg:gap-10 xl:gap-20">
        <section className="hidden min-w-0 items-center justify-center lg:flex" aria-label="Identidad visual de Padelito">
          <div className="login-ball-float w-full max-w-[820px]">
            <img
              alt="Pelota de pádel premium verde suspendida entre fragmentos de vidrio"
              className="aspect-square w-full object-cover [mask-image:radial-gradient(circle,black_42%,transparent_78%)] transition-transform duration-700 ease-out hover:scale-[1.015]"
              src={padelBallHero}
            />
          </div>
        </section>

        <section className="flex w-full flex-col justify-center justify-self-center lg:justify-self-end">
          <form
            className="w-full rounded-2xl border border-white/[0.09] bg-[#0d0f11]/95 px-6 py-9 shadow-[inset_0_1px_0_rgba(255,255,255,0.025),0_28px_80px_rgba(0,35,24,0.08)] backdrop-blur-xl sm:px-10 sm:py-11 xl:px-[60px] xl:py-[72px]"
            onSubmit={onSubmit}
          >
            <header className="mb-10 xl:mb-[62px]">
              <h1 className="font-display text-[42px] font-extrabold leading-[1.08] tracking-[-0.035em] text-[#50dfa8] sm:text-5xl">
                Padelito v2
              </h1>
              <p className="mt-3 text-base font-medium text-[#c0c4c2] sm:text-lg">
                Sistema exclusivo para personal autorizado
              </p>
            </header>

            <div className="space-y-7">
              <label className="block" htmlFor="username">
                <span className="text-xs font-bold uppercase tracking-[0.055em] text-[#c5cac7]">Usuario</span>
                <span className="relative mt-3 block">
                  <UserRound
                    aria-hidden="true"
                    className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-[#6f7580]"
                    strokeWidth={2}
                  />
                  <input
                    autoComplete="username"
                    className="h-16 w-full rounded-xl border border-[#69707a] bg-[#090b0d] pl-[50px] pr-4 text-base font-medium text-[#dce3ed] caret-[#50dfa8] outline-none transition-[border-color,box-shadow] placeholder:text-[#9298a4] hover:border-[#858d98] focus:border-[#50dfa8] focus:ring-2 focus:ring-[#50dfa8]/20"
                    id="username"
                    placeholder="admin@club.com"
                    {...register('username')}
                  />
                </span>
                {errors.username?.message ? (
                  <span className="mt-2 block text-sm font-medium text-[#ffb4ab]">{errors.username.message}</span>
                ) : null}
              </label>

              <label className="block" htmlFor="password">
                <span className="text-xs font-bold uppercase tracking-[0.055em] text-[#c5cac7]">Contraseña</span>
                <span className="relative mt-3 block">
                  <LockKeyhole
                    aria-hidden="true"
                    className="pointer-events-none absolute left-4 top-1/2 size-5 -translate-y-1/2 text-[#6f7580]"
                    strokeWidth={2}
                  />
                  <input
                    autoComplete="current-password"
                    className="h-16 w-full rounded-xl border border-[#69707a] bg-[#090b0d] pl-[50px] pr-4 text-base font-semibold tracking-[0.13em] text-[#dce3ed] caret-[#50dfa8] outline-none transition-[border-color,box-shadow] placeholder:text-[#dce3ed] hover:border-[#858d98] focus:border-[#50dfa8] focus:ring-2 focus:ring-[#50dfa8]/20"
                    id="password"
                    type="password"
                    {...register('password')}
                  />
                </span>
                {errors.password?.message ? (
                  <span className="mt-2 block text-sm font-medium text-[#ffb4ab]">{errors.password.message}</span>
                ) : null}
              </label>
            </div>

            {formError ? (
              <div
                aria-live="polite"
                className="mt-6 rounded-xl border border-[#ffb4ab]/25 bg-[#93000a]/20 px-4 py-3 text-sm font-medium text-[#ffdad6]"
                role="alert"
              >
                {formError}
              </div>
            ) : null}

            <button
              className="group mt-11 flex h-[66px] w-full items-center justify-center gap-3 whitespace-nowrap rounded-xl bg-[linear-gradient(105deg,#078b65,#08a475)] px-5 text-xl font-bold text-[#f4fff9] shadow-[inset_0_1px_0_rgba(255,255,255,0.18),0_16px_34px_rgba(5,146,105,0.16)] transition-[filter,transform,box-shadow] hover:brightness-110 hover:shadow-[inset_0_1px_0_rgba(255,255,255,0.2),0_18px_40px_rgba(5,146,105,0.23)] active:translate-y-px disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isSubmitting}
              type="submit"
            >
              {isSubmitting ? (
                <>
                  <LoaderCircle aria-hidden="true" className="size-5 animate-spin" strokeWidth={2} />
                  Ingresando...
                </>
              ) : (
                <>
                  Ingresar
                  <ArrowRight
                    aria-hidden="true"
                    className="size-6 transition-transform duration-200 group-hover:translate-x-1"
                    strokeWidth={2}
                  />
                </>
              )}
            </button>
          </form>

          <footer className="mt-10 px-1 text-center text-xs font-semibold leading-6 text-[#8a909b] sm:text-sm lg:mt-14 lg:text-left">
            <p>© 2026 Padelito v2. Premium Club Management.</p>
          </footer>
        </section>
      </div>
    </main>
  )
}
