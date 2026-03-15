export interface Book {
  id: string
  title: string
  isbn: string
  authorId: string
  authorName: string
  categoryId: string
  categoryName: string
  totalCopies: number
  availableCopies: number
  createdAt: string
  updatedAt?: string
}

export interface BookAvailability {
  bookId: string
  title: string
  totalCopies: number
  availableCopies: number
  isAvailable: boolean
}

export interface CreateBookRequest {
  title: string
  isbn: string
  authorId: string
  categoryId: string
  totalCopies?: number
}

export interface UpdateBookRequest {
  title: string
  categoryId: string
  totalCopies: number
}
