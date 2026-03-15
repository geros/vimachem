import { format, formatDistanceToNow } from 'date-fns'

export const formatDate = (dateString: string | undefined): string => {
  if (!dateString) return '-'
  return format(new Date(dateString), 'MMM dd, yyyy')
}

export const formatDateTime = (dateString: string | undefined): string => {
  if (!dateString) return '-'
  return format(new Date(dateString), 'MMM dd, yyyy HH:mm')
}

export const formatRelativeTime = (dateString: string | undefined): string => {
  if (!dateString) return '-'
  return formatDistanceToNow(new Date(dateString), { addSuffix: true })
}

export const formatISBN = (isbn: string): string => {
  if (isbn.length === 13) {
    return `${isbn.slice(0, 3)}-${isbn.slice(3, 4)}-${isbn.slice(4, 6)}-${isbn.slice(6, 12)}-${isbn.slice(12)}`
  }
  if (isbn.length === 10) {
    return `${isbn.slice(0, 1)}-${isbn.slice(1, 4)}-${isbn.slice(4, 9)}-${isbn.slice(9)}`
  }
  return isbn
}
