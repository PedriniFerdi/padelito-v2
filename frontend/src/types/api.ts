export type ApiError = {
  message: string
  statusCode?: number
}

export type Role = 'Admin' | 'Reception' | 'Staff'

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
  expiresAt: string
  user: CurrentUser
}

export type PersonCatalogItem = {
  id: number
  firstName: string
  lastName: string
  dni: string
  phone: string
  email: string
  isActive: boolean
}

export type Client = PersonCatalogItem

export type ClientProfile = {
  clientId: number
  clientName: string
  dni: string
  phone: string
  email: string
  isActive: boolean
  totalReservations: number
  totalPaid: number
  pendingBalance: number
  favoriteDayName?: string | null
  favoriteStartTime?: string | null
  lastVisitDate?: string | null
  cancellationCount: number
}

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

export type ReservationStatus = 'Pending' | 'Confirmed' | 'Canceled' | 'Completed'

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
  paymentStatus: 'Unpaid' | 'Partially paid' | 'Paid'
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

export type OperationsReservation = {
  id: number
  reservationDate: string
  clientId: number
  clientName: string
  availableTurnId: number
  courtId: number
  courtName: string
  startTime: string
  endTime: string
  reservationStatusId: number
  status: ReservationStatus
  finalPrice: number
  totalPaid: number
  pendingBalance: number
  paymentStatus: 'Unpaid' | 'Partially paid' | 'Paid'
}

export type OperationsCourtTimeline = {
  courtId: number
  courtName: string
  reservations: OperationsReservation[]
}

export type OperationsBoard = {
  operationalDate: string
  generatedAt: string
  reservationsToday: number
  upcomingUnpaidCount: number
  startingSoonCount: number
  completedCount: number
  timelineByCourt: OperationsCourtTimeline[]
  upcomingUnpaidReservations: OperationsReservation[]
  startingSoonReservations: OperationsReservation[]
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

export type DashboardRevenueIntelligence = {
  dateFrom: string
  dateTo: string
  summary: {
    totalRevenue: number
    reservedValue: number
    pendingBalance: number
    cancellationRate: number
    averageOccupancyRate: number
  }
  courts: {
    courtId: number
    courtName: string
    reservedSlots: number
    availableSlots: number
    occupancyRate: number
    revenue: number
  }[]
  demand: {
    dayOfWeek: number
    dayName: string
    hour: number
    reservationCount: number
    occupancyRate: number
  }[]
  peakDemand: {
    dayOfWeek: number
    dayName: string
    hour: number
    reservationCount: number
    occupancyRate: number
  }[]
  offPeakDemand: {
    dayOfWeek: number
    dayName: string
    hour: number
    reservationCount: number
    occupancyRate: number
  }[]
  bestPromotion?: {
    promotionId: number
    promotionName: string
    reservationCount: number
    grossRevenue: number
    discountTotal: number
    collectedRevenue: number
  } | null
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
  paymentStatus: 'Unpaid' | 'Partially paid' | 'Paid'
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
