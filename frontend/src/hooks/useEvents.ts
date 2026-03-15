import { useQuery } from '@tanstack/react-query'
import { eventsApi } from '@/api/events'

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
