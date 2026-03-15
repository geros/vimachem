import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { categoriesApi } from '@/api/categories'
import type { CreateCategoryRequest, UpdateCategoryRequest } from '@/types/category'

const CATEGORIES_KEY = 'categories'

export const useCategories = () => useQuery({
  queryKey: [CATEGORIES_KEY],
  queryFn: async () => {
    const response = await categoriesApi.getAll()
    return response.data
  },
})

export const useCategory = (id: string) => useQuery({
  queryKey: [CATEGORIES_KEY, id],
  queryFn: async () => {
    const response = await categoriesApi.getById(id)
    return response.data
  },
  enabled: !!id,
})

export const useCreateCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateCategoryRequest) => categoriesApi.create(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] }),
  })
}

export const useUpdateCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCategoryRequest }) =>
      categoriesApi.update(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] }),
  })
}

export const useDeleteCategory = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => categoriesApi.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] }),
  })
}
