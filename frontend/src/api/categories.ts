import { catalogClient } from './client'
import type { Category, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/category'

export const categoriesApi = {
  getAll: () => catalogClient.get<Category[]>('/api/catalog/categories'),
  getById: (id: string) => catalogClient.get<Category>(`/api/catalog/categories/${id}`),
  create: (data: CreateCategoryRequest) =>
    catalogClient.post<Category>('/api/catalog/categories', data),
  update: (id: string, data: UpdateCategoryRequest) =>
    catalogClient.put<Category>(`/api/catalog/categories/${id}`, data),
  delete: (id: string) => catalogClient.delete(`/api/catalog/categories/${id}`),
}
