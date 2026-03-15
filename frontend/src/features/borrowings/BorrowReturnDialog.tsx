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
  IconButton,
} from '@mui/material'
import { Close as CloseIcon, CheckCircle as CheckIcon } from '@mui/icons-material'
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
  const [selectedBook, setSelectedBook] = useState<{ id: string; title: string; authorName: string; available: number } | null>(null)
  const [selectedCustomer, setSelectedCustomer] = useState<{ id: string; name: string; email: string } | null>(null)
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
      await borrowMutation.mutateAsync({ bookId: selectedBook.id, customerId: selectedCustomer.id })
      showSuccess('Book borrowed successfully')
      handleClose()
    } catch {
      showError('Failed to borrow book')
    }
  }

  const handleReturn = async () => {
    if (!selectedReturn) return
    try {
      await returnMutation.mutateAsync({ bookId: selectedReturn.bookId, data: { customerId: selectedReturn.customerId } })
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

  const returnDaysElapsed = selectedReturn
    ? Math.floor((Date.now() - new Date(selectedReturn.borrowedAt).getTime()) / 86400000)
    : 0

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        Borrow / Return Books
        <IconButton size="small" onClick={handleClose}>
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent>
        <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
          <Tab label="Borrow" value="borrow" />
          <Tab label="Return" value="return" />
        </Tabs>

        {activeTab === 'borrow' ? (
          <Box display="flex" flexDirection="column" gap={3}>
            <Autocomplete
              options={books?.map((b) => ({ id: b.id, title: b.title, authorName: b.authorName, available: b.availableCopies })) ?? []}
              getOptionLabel={(option) => `${option.title} — by ${option.authorName} (${option.available} available)`}
              value={selectedBook}
              onChange={(_, newValue) => setSelectedBook(newValue)}
              renderInput={(params) => <TextField {...params} label="Select Book" fullWidth />}
              isOptionEqualToValue={(option, value) => option.id === value.id}
            />

            <Autocomplete
              options={customers.map((p) => ({ id: p.id, name: `${p.firstName} ${p.lastName}`, email: p.email }))}
              getOptionLabel={(option) => `${option.name} (${option.email})`}
              value={selectedCustomer}
              onChange={(_, newValue) => setSelectedCustomer(newValue)}
              renderInput={(params) => <TextField {...params} label="Select Customer" fullWidth />}
              isOptionEqualToValue={(option, value) => option.id === value.id}
            />

            {selectedBook && selectedCustomer && availability && availability.isAvailable && (
              <Box
                sx={{
                  p: 2,
                  borderRadius: 2,
                  backgroundColor: '#EFF6FF',
                  border: '1px solid #BFDBFE',
                }}
              >
                <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                  Lending "{selectedBook.title}" to {selectedCustomer.name}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  {availability.availableCopies} of {availability.totalCopies} copies available after this borrow
                </Typography>
              </Box>
            )}

            {selectedBook && !availability?.isAvailable && (
              <Alert severity="warning">This book is currently not available for borrowing.</Alert>
            )}
          </Box>
        ) : (
          <Box display="flex" flexDirection="column" gap={3}>
            <Autocomplete
              options={activeBorrowings}
              getOptionLabel={(option) =>
                `${option.bookTitle} - ${option.customerName} (borrowed ${new Date(option.borrowedAt).toLocaleDateString()})`
              }
              value={selectedReturn}
              onChange={(_, newValue) => setSelectedReturn(newValue ?? null)}
              renderInput={(params) => <TextField {...params} label="Select Borrowing to Return" fullWidth />}
              isOptionEqualToValue={(option, value) =>
                option.bookId === value?.bookId && option.customerId === value?.customerId
              }
            />

            {selectedReturn && (
              <Box
                sx={{
                  p: 2,
                  borderRadius: 2,
                  backgroundColor: '#F0FDF4',
                  border: '1px solid #BBF7D0',
                }}
              >
                <Typography variant="subtitle2" fontWeight={600} gutterBottom>
                  Returning "{selectedReturn.bookTitle}" from {selectedReturn.customerName}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Borrowed on {new Date(selectedReturn.borrowedAt).toLocaleDateString()} · {returnDaysElapsed} day{returnDaysElapsed !== 1 ? 's' : ''} elapsed
                </Typography>
                {returnDaysElapsed > 14 && (
                  <Chip label="Overdue" color="error" size="small" sx={{ mt: 1 }} />
                )}
              </Box>
            )}

            {activeBorrowings.length === 0 && (
              <Alert severity="info">No active borrowings to return.</Alert>
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
            startIcon={<CheckIcon />}
            disabled={!canBorrow || borrowMutation.isPending}
          >
            Confirm Borrow
          </Button>
        ) : (
          <Button
            onClick={handleReturn}
            variant="contained"
            color="success"
            startIcon={<CheckIcon />}
            disabled={!selectedReturn || returnMutation.isPending}
          >
            Confirm Return
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}

export default BorrowReturnDialog
