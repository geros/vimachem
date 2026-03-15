export interface Borrowing {
  id: string
  bookId: string
  bookTitle: string
  customerId: string
  customerName: string
  borrowedAt: string
  returnedAt?: string
  isActive: boolean
}

export interface BorrowerInfo {
  customerId: string
  customerName: string
  borrowedAt: string
}

export interface BorrowedBookSummary {
  bookId: string
  bookTitle: string
  borrowers: BorrowerInfo[]
}

export interface BorrowBookRequest {
  bookId: string
  customerId: string
}

export interface ReturnBookRequest {
  customerId: string
}
