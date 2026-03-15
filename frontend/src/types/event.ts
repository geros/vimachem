export interface LibraryEvent {
  id: string
  eventType: string
  entityType: string
  entityId: string
  action: string
  relatedEntityIds: Record<string, string>
  payload?: unknown
  timestamp: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
