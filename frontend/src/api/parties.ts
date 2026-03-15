import { partyClient } from './client'
import type { Party, CreatePartyRequest, UpdatePartyRequest, AssignRoleRequest, RoleType } from '@/types/party'

export const partiesApi = {
  getAll: () => partyClient.get<Party[]>('/api/parties'),
  getById: (id: string) => partyClient.get<Party>(`/api/parties/${id}`),
  create: (data: CreatePartyRequest) => partyClient.post<Party>('/api/parties', data),
  update: (id: string, data: UpdatePartyRequest) => partyClient.put<Party>(`/api/parties/${id}`, data),
  delete: (id: string) => partyClient.delete(`/api/parties/${id}`),
  assignRole: (id: string, data: AssignRoleRequest) =>
    partyClient.post<Party>(`/api/parties/${id}/roles`, data),
  removeRole: (id: string, roleType: RoleType) =>
    partyClient.delete<Party>(`/api/parties/${id}/roles/${roleType}`),
}
