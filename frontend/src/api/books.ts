import { catalogClient } from './client'
import type { Book, BookAvailability, CreateBookRequest, UpdateBookRequest } from '@/types/book'

export const booksApi = {
  getAll: () => catalogClient.get<Book[]>('/api/catalog/books'),
  getById: (id: string) => catalogClient.get<Book>(`/api/catalog/books/${id}`),
  searchByTitle: (title: string) =>
    catalogClient.get<Book[]>(`/api/catalog/books/search?title=${encodeURIComponent(title)}`),
  getAvailability: (id: string) =>
    catalogClient.get<BookAvailability>(`/api/catalog/books/${id}/availability`),
  create: (data: CreateBookRequest) => catalogClient.post<Book>('/api/catalog/books', data),
  update: (id: string, data: UpdateBookRequest) =>
    catalogClient.put<Book>(`/api/catalog/books/${id}`, data),
  delete: (id: string) => catalogClient.delete(`/api/catalog/books/${id}`),
}
