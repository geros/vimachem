import React, { createContext, useContext, useState, useCallback } from 'react'
import { Snackbar, Alert, type AlertColor } from '@mui/material'

interface Toast {
  id: string
  message: string
  severity: AlertColor
}

interface ToastContextType {
  showSuccess: (message: string) => void
  showError: (message: string) => void
  showWarning: (message: string) => void
  showInfo: (message: string) => void
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<Toast[]>([])

  const addToast = useCallback((message: string, severity: AlertColor) => {
    const id = Math.random().toString(36).substring(7)
    setToasts((prev) => [...prev, { id, message, severity }])
  }, [])

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const showSuccess = useCallback((message: string) => addToast(message, 'success'), [addToast])
  const showError = useCallback((message: string) => addToast(message, 'error'), [addToast])
  const showWarning = useCallback((message: string) => addToast(message, 'warning'), [addToast])
  const showInfo = useCallback((message: string) => addToast(message, 'info'), [addToast])

  return (
    <ToastContext.Provider value={{ showSuccess, showError, showWarning, showInfo }}>
      {children}
      {toasts.map((toast) => (
        <Snackbar
          key={toast.id}
          open
          autoHideDuration={5000}
          onClose={() => removeToast(toast.id)}
          anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
        >
          <Alert severity={toast.severity} onClose={() => removeToast(toast.id)}>
            {toast.message}
          </Alert>
        </Snackbar>
      ))}
    </ToastContext.Provider>
  )
}

export const useToast = () => {
  const context = useContext(ToastContext)
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider')
  }
  return context
}
