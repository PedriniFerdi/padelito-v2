export type ApiError = {
  message: string
  statusCode?: number
}

export type Role = 'Administrador' | 'Recepcion' | 'Empleado'

export type CurrentUser = {
  userId: number
  username: string
  employeeId: number
  role: Role
  clubId: number
}

export type LoginRequest = {
  username: string
  password: string
}

export type AuthResponse = {
  token: string
  expiresAt: string
  user: CurrentUser
}

export type PersonCatalogItem = {
  id: number
  firstName: string
  lastName: string
  dni?: string | null
  phone?: string | null
  email?: string | null
  isActive: boolean
}

export type Client = PersonCatalogItem

export type Employee = PersonCatalogItem & {
  hasUser: boolean
}

export type RoleCatalog = {
  id: number
  name: Role
}

export type UserCatalog = {
  id: number
  username: string
  employeeId: number
  employeeName: string
  roleId: number
  role: Role
  isActive: boolean
}

export type CourtType = {
  id: number
  description: string
}

export type Court = {
  id: number
  name: string
  courtTypeId: number
  courtType: string
  hourPrice: number
  isActive: boolean
}

export type AvailableTurn = {
  id: number
  courtId: number
  courtName: string
  startTime: string
  endTime: string
  isActive: boolean
}

export type Promotion = {
  id: number
  name: string
  description?: string | null
  discountPercentage: number
  dateFrom: string
  dateTo: string
  isActive: boolean
}

export type ReservationStatus = 'Pendiente' | 'Confirmada' | 'Cancelada' | 'Finalizada'

export type Reservation = {
  id: number
  reservationDate: string
  clientId: number
  clientName: string
  availableTurnId: number
  courtName: string
  startTime: string
  endTime: string
  reservationStatusId: number
  status: ReservationStatus
  promotionName?: string | null
  basePrice: number
  finalPrice: number
  createdAt: string
}

export type ReservationDetail = Reservation & {
  courtId: number
  courtType: string
  employeeId: number
  employeeName: string
  promotionId?: number | null
  discountPercentage?: number | null
  totalPaid: number
  pendingBalance: number
  paymentStatus: 'Sin pagos' | 'Pago parcial' | 'Pagada'
}

export type ReservationAvailability = {
  availableTurnId: number
  courtId: number
  courtName: string
  courtType: string
  startTime: string
  endTime: string
  basePrice: number
}

export type PaymentMethod = { id: number; description: string }

export type Payment = {
  id: number
  reservationId: number
  reservationDate: string
  clientName: string
  courtName: string
  paymentMethodId: number
  paymentMethod: string
  amount: number
  paymentDate: string
  note?: string | null
  finalPrice: number
  totalPaid: number
  pendingBalance: number
}

export type DashboardReservation = {
  id: number
  reservationDate: string
  clientName: string
  courtName: string
  startTime: string
  status: ReservationStatus
  finalPrice: number
}

export type DashboardSummary = {
  operationalDate: string
  activeClients: number
  activeCourts: number
  reservationsToday: number
  incomeToday: number
  latestReservations: DashboardReservation[]
}

export type ReservationReportRow = {
  reservationId: number
  reservationDate: string
  startTime: string
  endTime: string
  clientName: string
  courtName: string
  reservationStatusId: number
  status: ReservationStatus
  promotionName?: string | null
  basePrice: number
  finalPrice: number
  totalPaid: number
  pendingBalance: number
  paymentStatus: 'Sin pagos' | 'Pago parcial' | 'Pagada'
}

export type ReservationReport = {
  summary: { reservationCount: number; finalPriceTotal: number; totalPaid: number; pendingBalance: number }
  rows: ReservationReportRow[]
}

export type ReservationAudit = {
  id: number
  reservationId: number
  reservationDate: string
  clientName: string
  courtName: string
  action: string
  description: string
  username: string
  createdAt: string
}
