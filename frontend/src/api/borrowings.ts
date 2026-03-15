import { lendingClient } from './client'
import type { Borrowing, BorrowedBookSummary, BorrowBookRequest, ReturnBookRequest } from '@/types/borrowing'

export const borrowingsApi = {
  getById: (id: string) => lendingClient.get<Borrowing>(`/api/lending/${id}`),
  getByCustomer: (customerId: string) =>
    lendingClient.get<Borrowing[]>(`/api/lending/by-customer/${customerId}`),
  getByBook: (bookId: string) =>
    lendingClient.get<Borrowing[]>(`/api/lending/by-book/${bookId}`),
  getSummary: () => lendingClient.get<BorrowedBookSummary[]>('/api/lending/summary'),
  borrow: (data: BorrowBookRequest) =>
    lendingClient.post<Borrowing>('/api/lending/borrow', data),
  return: (bookId: string, data: ReturnBookRequest) =>
    lendingClient.post<Borrowing>(`/api/lending/${bookId}/return`, data),
}
