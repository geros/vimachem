import { auditClient } from './client'
import type { EventFilter, LibraryEvent, PagedResponse } from '@/types/event'

export const eventsApi = {
  getPartyEvents: (partyId: string, page = 1, pageSize = 20) =>
    auditClient.get<PagedResponse<LibraryEvent>>(
      `/api/events/parties/${partyId}?page=${page}&pageSize=${pageSize}`
    ),
  getBookEvents: (bookId: string, page = 1, pageSize = 20) =>
    auditClient.get<PagedResponse<LibraryEvent>>(
      `/api/events/books/${bookId}?page=${page}&pageSize=${pageSize}`
    ),
  getAllEvents: (filter: EventFilter, page = 1, pageSize = 20) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (filter.entityType) params.set('entityType', filter.entityType)
    if (filter.action)     params.set('action',     filter.action)
    if (filter.entityId)   params.set('entityId',   filter.entityId)
    if (filter.from)       params.set('from',        filter.from)
    if (filter.to)         params.set('to',          filter.to)
    return auditClient.get<PagedResponse<LibraryEvent>>(`/api/events?${params}`)
  },
}
