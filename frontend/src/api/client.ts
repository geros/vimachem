import axios, { AxiosError, AxiosInstance } from 'axios'

const createClient = (baseURL: string): AxiosInstance => {
  const client = axios.create({
    baseURL,
    headers: {
      'Content-Type': 'application/json',
    },
    timeout: 10000,
  })

  client.interceptors.request.use(
    (config) => config,
    (error) => Promise.reject(error)
  )

  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      if (error.response) {
        console.error('API Error:', error.response.status, error.response.data)
      } else if (error.request) {
        console.error('Network Error:', error.message)
      }
      return Promise.reject(error)
    }
  )

  return client
}

export const partyClient = createClient('')
export const catalogClient = createClient('')
export const lendingClient = createClient('')
export const auditClient = createClient('')

export default createClient
