import { z } from 'zod'

const requiredText = (label: string, max: number) =>
  z.string().trim().min(1, `${label} is required.`).max(max, `${label} cannot exceed ${max} characters.`)

export const personSchema = z.object({
  firstName: requiredText('First name', 60),
  lastName: requiredText('Last name', 60),
  dni: z.string().trim().min(1, 'Customer ID is required.').transform(value => value.replace(/[.\s]/g, '')).pipe(z.string().regex(/^\d{7,10}$/, 'Customer ID must contain 7 to 10 digits.')),
  phone: z.string().trim().min(8, 'Phone must contain at least 8 characters.').max(40, 'Phone cannot exceed 40 characters.').regex(/^\+?[0-9 ()-]+$/, 'Phone has an invalid format.').refine(value => value.replace(/\D/g, '').length >= 8, 'Phone must contain at least 8 digits.'),
  email: z.string().trim().min(1, 'Email is required.').max(120, 'Email cannot exceed 120 characters.').email('Email has an invalid format.').transform(value => value.toLowerCase()),
})

export const userSchema = z.object({
  username: requiredText('Username', 50),
  password: z.string().min(8, 'Password must contain at least 8 characters.').max(100, 'Password cannot exceed 100 characters.'),
  employeeId: z.number().int().positive('Select a staff member.'),
  roleId: z.number().int().positive('Select a role.'),
})

export const userUpdateSchema = userSchema.omit({ password: true, employeeId: true })
export const passwordSchema = userSchema.shape.password
export const courtTypeSchema = requiredText('Description', 80)

export const courtSchema = z.object({
  name: requiredText('Court name', 80),
  courtTypeId: z.number().int().positive('Select a court type.'),
  hourPrice: z.number().finite().positive('Hourly price must be greater than zero.'),
})

export const turnSchema = z.object({
  courtId: z.number().int().positive('Select a court.'),
  startTime: z.string().min(1, 'Start time is required.'),
  endTime: z.string().min(1, 'End time is required.'),
}).refine(values => values.endTime > values.startTime, { path: ['endTime'], message: 'End time must be after start time.' })

export const promotionSchema = z.object({
  name: requiredText('Promotion name', 80),
  description: z.string().trim().max(255, 'Description cannot exceed 255 characters.').transform(value => value || null),
  discountPercentage: z.number().finite().positive('Discount must be greater than zero.').max(100, 'Discount cannot exceed 100%.'),
  dateFrom: z.string().min(1, 'Start date is required.'),
  dateTo: z.string().min(1, 'End date is required.'),
}).refine(values => values.dateTo >= values.dateFrom, { path: ['dateTo'], message: 'End date must be on or after start date.' })

export const reservationSchema = z.object({
  clientId: z.number().int().positive('Select a customer.'),
  availableTurnId: z.number().int().positive('Select a time slot.'),
  promotionId: z.number().int().positive().nullable(),
  reservationDate: z.string().min(1, 'Date is required.'),
  reservationStatusId: z.number().int().positive('Select a status.'),
})

export const paymentSchema = z.object({
  reservationId: z.number().int().positive('Select a reservation.'),
  paymentMethodId: z.number().int().positive('Select a payment method.'),
  amount: z.number().finite().positive('Amount must be greater than zero.'),
  pendingBalance: z.number().nonnegative(),
  note: z.string().trim().max(255, 'Note cannot exceed 255 characters.'),
}).refine(values => values.amount <= values.pendingBalance, { path: ['amount'], message: 'Amount cannot exceed the outstanding balance.' })

export type FieldErrors = Record<string, string>

export function toFieldErrors(error: z.ZodError): FieldErrors {
  return error.issues.reduce<FieldErrors>((errors, issue) => {
    const field = String(issue.path[0] ?? 'form')
    errors[field] ??= issue.message
    return errors
  }, {})
}
