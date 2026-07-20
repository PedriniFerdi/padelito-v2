import { z } from 'zod'

const requiredText = (label: string, max: number) =>
  z.string().trim().min(1, `${label} es obligatorio.`).max(max, `${label} no puede superar los ${max} caracteres.`)

export const personSchema = z.object({
  firstName: requiredText('El nombre', 60),
  lastName: requiredText('El apellido', 60),
  dni: z.string().trim().min(1, 'El DNI es obligatorio.').transform(value => value.replace(/[.\s]/g, '')).pipe(z.string().regex(/^\d{7,8}$/, 'El DNI debe tener 7 u 8 dígitos.')),
  phone: z.string().trim().min(8, 'El teléfono debe tener al menos 8 caracteres.').max(40, 'El teléfono no puede superar los 40 caracteres.').regex(/^\+?[0-9 ()-]+$/, 'El teléfono tiene un formato inválido.').refine(value => value.replace(/\D/g, '').length >= 8, 'El teléfono debe contener al menos 8 dígitos.'),
  email: z.string().trim().min(1, 'El email es obligatorio.').max(120, 'El email no puede superar los 120 caracteres.').email('El email tiene un formato inválido.').transform(value => value.toLowerCase()),
})

export const userSchema = z.object({
  username: requiredText('El username', 50),
  password: z.string().min(8, 'La contraseña debe tener al menos 8 caracteres.').max(100, 'La contraseña no puede superar los 100 caracteres.'),
  employeeId: z.number().int().positive('Debe seleccionar un empleado.'),
  roleId: z.number().int().positive('Debe seleccionar un rol.'),
})

export const userUpdateSchema = userSchema.omit({ password: true, employeeId: true })
export const passwordSchema = userSchema.shape.password
export const courtTypeSchema = requiredText('La descripción', 80)

export const courtSchema = z.object({
  name: requiredText('El nombre', 80),
  courtTypeId: z.number().int().positive('Debe seleccionar un tipo de cancha.'),
  hourPrice: z.number().finite().positive('El precio por hora debe ser mayor a cero.'),
})

export const turnSchema = z.object({
  courtId: z.number().int().positive('Debe seleccionar una cancha.'),
  startTime: z.string().min(1, 'La hora de inicio es obligatoria.'),
  endTime: z.string().min(1, 'La hora de fin es obligatoria.'),
}).refine(values => values.endTime > values.startTime, { path: ['endTime'], message: 'La hora de fin debe ser posterior a la hora de inicio.' })

export const promotionSchema = z.object({
  name: requiredText('El nombre', 80),
  description: z.string().trim().max(255, 'La descripción no puede superar los 255 caracteres.').transform(value => value || null),
  discountPercentage: z.number().finite().positive('El descuento debe ser mayor a cero.').max(100, 'El descuento no puede superar el 100%.'),
  dateFrom: z.string().min(1, 'La fecha desde es obligatoria.'),
  dateTo: z.string().min(1, 'La fecha hasta es obligatoria.'),
}).refine(values => values.dateTo >= values.dateFrom, { path: ['dateTo'], message: 'La fecha hasta debe ser igual o posterior a la fecha desde.' })

export const reservationSchema = z.object({
  clientId: z.number().int().positive('Debe seleccionar un cliente.'),
  availableTurnId: z.number().int().positive('Debe seleccionar un turno.'),
  promotionId: z.number().int().positive().nullable(),
  reservationDate: z.string().min(1, 'La fecha es obligatoria.'),
  reservationStatusId: z.number().int().positive('Debe seleccionar un estado.'),
})

export const paymentSchema = z.object({
  reservationId: z.number().int().positive('Debe seleccionar una reserva.'),
  paymentMethodId: z.number().int().positive('Debe seleccionar un método de pago.'),
  amount: z.number().finite().positive('El monto debe ser mayor a cero.'),
  pendingBalance: z.number().nonnegative(),
  note: z.string().trim().max(255, 'La nota no puede superar los 255 caracteres.'),
}).refine(values => values.amount <= values.pendingBalance, { path: ['amount'], message: 'El monto no puede superar el saldo pendiente.' })

export type FieldErrors = Record<string, string>

export function toFieldErrors(error: z.ZodError): FieldErrors {
  return error.issues.reduce<FieldErrors>((errors, issue) => {
    const field = String(issue.path[0] ?? 'form')
    errors[field] ??= issue.message
    return errors
  }, {})
}
