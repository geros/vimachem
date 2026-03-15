import { auditClient } from './client'
import type { LibraryEvent, PagedResponse } from '@/types/event'

export const eventsApi = {
  getPartyEvents: (partyId: string, page = 1, pageSize = 20) =>
    auditClient.get<PagedResponse<LibraryEvent>>(
      `/api/events/parties/${partyId}?page=${page}&pageSize=${pageSize}`
    ),
  getBookEvents: (bookId: string, page = 1, pageSize = 20) =>
    auditClient.get<PagedResponse<LibraryEvent>>(
      `/api/events/books/${bookId}?page=${page}&pageSize=${pageSize}`
    ),
}
