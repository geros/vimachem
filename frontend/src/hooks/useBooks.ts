import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { booksApi } from '@/api/books'
import type { CreateBookRequest, UpdateBookRequest } from '@/types/book'

const BOOKS_KEY = 'books'

export const useBooks = () => useQuery({
  queryKey: [BOOKS_KEY],
  queryFn: async () => {
    const response = await booksApi.getAll()
    return response.data
  },
})

export const useBook = (id: string) => useQuery({
  queryKey: [BOOKS_KEY, id],
  queryFn: async () => {
    const response = await booksApi.getById(id)
    return response.data
  },
  enabled: !!id,
})

export const useBookAvailability = (id: string) => useQuery({
  queryKey: [BOOKS_KEY, id, 'availability'],
  queryFn: async () => {
    const response = await booksApi.getAvailability(id)
    return response.data
  },
  enabled: !!id,
})

export const useSearchBooks = (title: string) => useQuery({
  queryKey: [BOOKS_KEY, 'search', title],
  queryFn: async () => {
    const response = await booksApi.searchByTitle(title)
    return response.data
  },
  enabled: title.length > 0,
})

export const useCreateBook = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateBookRequest) => booksApi.create(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [BOOKS_KEY] }),
  })
}

export const useUpdateBook = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBookRequest }) =>
      booksApi.update(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [BOOKS_KEY] }),
  })
}

export const useDeleteBook = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => booksApi.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [BOOKS_KEY] }),
  })
}
