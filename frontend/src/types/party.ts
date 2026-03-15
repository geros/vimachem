export const RoleType = {
  Author: 0,
  Customer: 1,
} as const

export type RoleType = typeof RoleType[keyof typeof RoleType]

export interface Party {
  id: string
  firstName: string
  lastName: string
  email: string
  roles: string[]
  createdAt: string
  updatedAt?: string
}

export interface CreatePartyRequest {
  firstName: string
  lastName: string
  email: string
}

export interface UpdatePartyRequest {
  firstName: string
  lastName: string
  email: string
}

export interface AssignRoleRequest {
  roleType: RoleType
}
