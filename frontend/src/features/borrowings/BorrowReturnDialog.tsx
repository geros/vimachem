import { useState } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Tabs,
  Tab,
  Box,
  Autocomplete,
  TextField,
  Typography,
  Alert,
  Chip,
} from '@mui/material'
import { useBooks } from '@/hooks/useBooks'
import { useParties } from '@/hooks/useParties'
import { useBookAvailability } from '@/hooks/useBooks'
import { useBorrowBook, useReturnBook, useBorrowingSummary } from '@/hooks/useBorrowings'
import { useToast } from '@/context/ToastContext'

interface BorrowReturnDialogProps {
  open: boolean
  onClose: () => void
  initialTab?: 'borrow' | 'return'
}

const BorrowReturnDialog: React.FC<BorrowReturnDialogProps> = ({
  open,
  onClose,
  initialTab = 'borrow',
}) => {
  const [activeTab, setActiveTab] = useState(initialTab)
  const [selectedBook, setSelectedBook] = useState<{ id: string; title: string; available: number } | null>(null)
  const [selectedCustomer, setSelectedCustomer] = useState<{ id: string; name: string } | null>(null)
  const [selectedReturn, setSelectedReturn] = useState<{ bookId: string; customerId: string; bookTitle: string; customerName: string; borrowedAt: string } | null>(null)

  const { data: books } = useBooks()
  const { data: parties } = useParties()
  const { data: availability } = useBookAvailability(selectedBook?.id ?? '')
  const { data: summary } = useBorrowingSummary()
  const { showSuccess, showError } = useToast()

  const borrowMutation = useBorrowBook()
  const returnMutation = useReturnBook()

  const handleTabChange = (_: React.SyntheticEvent, newValue: 'borrow' | 'return') => {
    setActiveTab(newValue)
    setSelectedBook(null)
    setSelectedCustomer(null)
    setSelectedReturn(null)
  }

  const handleBorrow = async () => {
    if (!selectedBook || !selectedCustomer) return

    try {
      await borrowMutation.mutateAsync({
        bookId: selectedBook.id,
        customerId: selectedCustomer.id,
      })
      showSuccess('Book borrowed successfully')
      handleClose()
    } catch {
      showError('Failed to borrow book')
    }
  }

  const handleReturn = async () => {
    if (!selectedReturn) return

    try {
      await returnMutation.mutateAsync({
        bookId: selectedReturn.bookId,
        data: { customerId: selectedReturn.customerId },
      })
      showSuccess('Book returned successfully')
      handleClose()
    } catch {
      showError('Failed to return book')
    }
  }

  const handleClose = () => {
    setSelectedBook(null)
    setSelectedCustomer(null)
    setSelectedReturn(null)
    onClose()
  }

  const activeBorrowings = summary?.flatMap((book) =>
    book.borrowers.map((borrower) => ({
      bookId: book.bookId,
      bookTitle: book.bookTitle,
      customerId: borrower.customerId,
      customerName: borrower.customerName,
      borrowedAt: borrower.borrowedAt,
    }))
  ) ?? []

  const customers = parties?.filter((p) => p.roles.includes('Customer')) ?? []

  const canBorrow = selectedBook && selectedCustomer && availability?.isAvailable

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Borrow / Return Books</DialogTitle>
      <DialogContent>
        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
          <Tab label="Borrow" value="borrow" />
          <Tab label="Return" value="return" />
        </Tabs>

        {activeTab === 'borrow' ? (
          <Box display="flex" flexDirection="column" gap={3}>
            <Autocomplete
              options={books?.map((b) => ({ id: b.id, title: b.title, available: b.availableCopies })) ?? []}
              getOptionLabel={(option) => `${option.title} (${option.available} available)`}
              value={selectedBook}
              onChange={(_, newValue) => setSelectedBook(newValue)}
              renderInput={(params) => (
                <TextField {...params} label="Select Book" fullWidth />
              )}
              isOptionEqualToValue={(option, value) => option.id === value.id}
            />

            {selectedBook && availability && (
              <Box>
                <Typography variant="subtitle2">Availability:</Typography>
                <Chip
                  label={availability.isAvailable ? 'Available' : 'Not Available'}
                  color={availability.isAvailable ? 'success' : 'error'}
                  size="small"
                />
                <Typography variant="body2" color="textSecondary">
                  {availability.availableCopies} of {availability.totalCopies} copies available
                </Typography>
              </Box>
            )}

            <Autocomplete
              options={customers.map((p) => ({
                id: p.id,
                name: `${p.firstName} ${p.lastName}`,
              }))}
              getOptionLabel={(option) => option.name}
              value={selectedCustomer}
              onChange={(_, newValue) => setSelectedCustomer(newValue)}
              renderInput={(params) => (
                <TextField {...params} label="Select Customer" fullWidth />
              )}
              isOptionEqualToValue={(option, value) => option.id === value.id}
            />

            {!availability?.isAvailable && selectedBook && (
              <Alert severity="warning">This book is currently not available for borrowing.</Alert>
            )}
          </Box>
        ) : (
          <Box>
            <Autocomplete
              options={activeBorrowings}
              getOptionLabel={(option) =>
                `${option.bookTitle} - ${option.customerName} (borrowed ${new Date(option.borrowedAt).toLocaleDateString()})`
              }
              value={selectedReturn}
              onChange={(_, newValue) => {
                if (newValue) {
                  setSelectedReturn({
                    bookId: newValue.bookId,
                    customerId: newValue.customerId,
                    bookTitle: newValue.bookTitle,
                    customerName: newValue.customerName,
                    borrowedAt: newValue.borrowedAt,
                  })
                } else {
                  setSelectedReturn(null)
                }
              }}
              renderInput={(params) => (
                <TextField {...params} label="Select Borrowing to Return" fullWidth />
              )}
              isOptionEqualToValue={(option, value) =>
                option.bookId === value?.bookId && option.customerId === value?.customerId
              }
            />

            {activeBorrowings.length === 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
                No active borrowings to return.
              </Alert>
            )}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        {activeTab === 'borrow' ? (
          <Button
            onClick={handleBorrow}
            variant="contained"
            disabled={!canBorrow || borrowMutation.isPending}
          >
            Borrow
          </Button>
        ) : (
          <Button
            onClick={handleReturn}
            variant="contained"
            color="success"
            disabled={!selectedReturn || returnMutation.isPending}
          >
            Return
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}

export default BorrowReturnDialog
