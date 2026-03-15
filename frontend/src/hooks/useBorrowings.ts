import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { borrowingsApi } from '@/api/borrowings'
import type { BorrowBookRequest, ReturnBookRequest } from '@/types/borrowing'

const BORROWINGS_KEY = 'borrowings'

export const useBorrowingSummary = () => useQuery({
  queryKey: [BORROWINGS_KEY, 'summary'],
  queryFn: async () => {
    const response = await borrowingsApi.getSummary()
    return response.data
  },
})

export const useBorrowing = (id: string) => useQuery({
  queryKey: [BORROWINGS_KEY, id],
  queryFn: async () => {
    const response = await borrowingsApi.getById(id)
    return response.data
  },
  enabled: !!id,
})

export const useBorrowingsByCustomer = (customerId: string) => useQuery({
  queryKey: [BORROWINGS_KEY, 'customer', customerId],
  queryFn: async () => {
    const response = await borrowingsApi.getByCustomer(customerId)
    return response.data
  },
  enabled: !!customerId,
})

export const useBorrowingsByBook = (bookId: string) => useQuery({
  queryKey: [BORROWINGS_KEY, 'book', bookId],
  queryFn: async () => {
    const response = await borrowingsApi.getByBook(bookId)
    return response.data
  },
  enabled: !!bookId,
})

export const useBorrowBook = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: BorrowBookRequest) => borrowingsApi.borrow(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [BORROWINGS_KEY] })
      queryClient.invalidateQueries({ queryKey: ['books'] })
    },
  })
}

export const useReturnBook = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ bookId, data }: { bookId: string; data: ReturnBookRequest }) =>
      borrowingsApi.return(bookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [BORROWINGS_KEY] })
      queryClient.invalidateQueries({ queryKey: ['books'] })
    },
  })
}
