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
