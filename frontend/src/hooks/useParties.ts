import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { partiesApi } from '@/api/parties'
import type { CreatePartyRequest, UpdatePartyRequest, AssignRoleRequest, RoleType } from '@/types/party'

const PARTIES_KEY = 'parties'

export const useParties = () => useQuery({
  queryKey: [PARTIES_KEY],
  queryFn: async () => {
    const response = await partiesApi.getAll()
    return response.data
  },
})

export const useParty = (id: string) => useQuery({
  queryKey: [PARTIES_KEY, id],
  queryFn: async () => {
    const response = await partiesApi.getById(id)
    return response.data
  },
  enabled: !!id,
})

export const useCreateParty = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreatePartyRequest) => partiesApi.create(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PARTIES_KEY] }),
  })
}

export const useUpdateParty = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePartyRequest }) =>
      partiesApi.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY] })
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY, id] })
    },
  })
}

export const useDeleteParty = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => partiesApi.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [PARTIES_KEY] }),
  })
}

export const useAssignRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AssignRoleRequest }) =>
      partiesApi.assignRole(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY] })
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY, id] })
    },
  })
}

export const useRemoveRole = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roleType }: { id: string; roleType: RoleType }) =>
      partiesApi.removeRole(id, roleType),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY] })
      queryClient.invalidateQueries({ queryKey: [PARTIES_KEY, id] })
    },
  })
}
