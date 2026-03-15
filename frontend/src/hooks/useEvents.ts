import { useQuery } from '@tanstack/react-query'
import { eventsApi } from '@/api/events'
import type { EventFilter } from '@/types/event'

const EVENTS_KEY = 'events'

export const usePartyEvents = (partyId: string, page = 1, pageSize = 20) => useQuery({
  queryKey: [EVENTS_KEY, 'party', partyId, page, pageSize],
  queryFn: async () => {
    const response = await eventsApi.getPartyEvents(partyId, page, pageSize)
    return response.data
  },
  enabled: !!partyId,
})

export const useBookEvents = (bookId: string, page = 1, pageSize = 20) => useQuery({
  queryKey: [EVENTS_KEY, 'book', bookId, page, pageSize],
  queryFn: async () => {
    const response = await eventsApi.getBookEvents(bookId, page, pageSize)
    return response.data
  },
  enabled: !!bookId,
})

export const useAllEvents = (filter: EventFilter, page = 1, pageSize = 20) => useQuery({
  queryKey: [EVENTS_KEY, 'all', filter, page, pageSize],
  queryFn: async () => {
    const response = await eventsApi.getAllEvents(filter, page, pageSize)
    return response.data
  },
})
