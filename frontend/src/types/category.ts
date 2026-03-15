export interface Category {
  id: string
  name: string
  bookCount: number
  createdAt: string
}

export interface CreateCategoryRequest {
  name: string
}

export interface UpdateCategoryRequest {
  name: string
}
