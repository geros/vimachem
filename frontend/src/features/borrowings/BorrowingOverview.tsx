import { useState } from 'react'
import {
  Box,
  Typography,
  Button,
  ToggleButton,
  ToggleButtonGroup,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  TextField,
} from '@mui/material'
import {
  ExpandMore as ExpandMoreIcon,
  Add as AddIcon,
  MenuBook as MenuBookIcon,
  AssignmentReturn as ReturnIcon,
} from '@mui/icons-material'
import { useBorrowingSummary, useReturnBook } from '@/hooks/useBorrowings'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { useToast } from '@/context/ToastContext'
import BorrowReturnDialog from './BorrowReturnDialog'

type ViewMode = 'grouped' | 'flat'

const getDaysBorrowed = (borrowedAt: string) => {
  const days = Math.floor((Date.now() - new Date(borrowedAt).getTime()) / 86400000)
  return days
}

const isOverdue = (borrowedAt: string) => getDaysBorrowed(borrowedAt) > 14

const BorrowingOverview: React.FC = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('grouped')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogTab, setDialogTab] = useState<'borrow' | 'return'>('borrow')
  const [searchTerm, setSearchTerm] = useState('')

  const { data: summary, isLoading } = useBorrowingSummary()
  const returnMutation = useReturnBook()
  const { showSuccess, showError } = useToast()

  const handleViewModeChange = (_: React.MouseEvent<HTMLElement>, newMode: ViewMode) => {
    if (newMode !== null) setViewMode(newMode)
  }

  const handleReturn = async (bookId: string, customerId: string) => {
    try {
      await returnMutation.mutateAsync({ bookId, data: { customerId } })
      showSuccess('Book returned successfully')
    } catch {
      showError('Failed to return book')
    }
  }

  if (isLoading) return <LoadingSkeleton rows={5} columns={4} />

  const flatBorrowings = summary?.flatMap((book) =>
    book.borrowers.map((borrower) => ({
      bookId: book.bookId,
      bookTitle: book.bookTitle,
      ...borrower,
    }))
  ) ?? []

  const filteredSummary = summary?.filter((book) => {
    if (!searchTerm) return true
    return (
      book.bookTitle.toLowerCase().includes(searchTerm.toLowerCase()) ||
      book.borrowers.some((b) => b.customerName.toLowerCase().includes(searchTerm.toLowerCase()))
    )
  }) ?? []

  const filteredFlat = flatBorrowings.filter((b) => {
    if (!searchTerm) return true
    return (
      b.bookTitle.toLowerCase().includes(searchTerm.toLowerCase()) ||
      b.customerName.toLowerCase().includes(searchTerm.toLowerCase())
    )
  })

  const totalBorrowings = summary?.reduce((sum, b) => sum + b.borrowers.length, 0) ?? 0

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
        <Box>
          <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
            Borrowings
          </Typography>
          <Typography variant="body2" color="textSecondary">
            {totalBorrowings} book{totalBorrowings !== 1 ? 's' : ''} currently borrowed
          </Typography>
        </Box>
        <Box display="flex" gap={2} alignItems="center">
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={handleViewModeChange}
            size="small"
          >
            <ToggleButton value="grouped">Grouped</ToggleButton>
            <ToggleButton value="flat">Flat</ToggleButton>
          </ToggleButtonGroup>
          <Button
            variant="outlined"
            startIcon={<ReturnIcon />}
            onClick={() => { setDialogTab('return'); setDialogOpen(true) }}
          >
            Return
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => { setDialogTab('borrow'); setDialogOpen(true) }}
          >
            Borrow Book
          </Button>
        </Box>
      </Box>

      <Box sx={{ mb: 3, mt: 2 }}>
        <TextField
          placeholder="Search by book title or customer name..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ width: 360 }}
        />
      </Box>

      {viewMode === 'grouped' ? (
        <Box>
          {filteredSummary.map((book) => (
            <Accordion key={book.bookId}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Box display="flex" alignItems="center" gap={2}>
                  <MenuBookIcon color="action" />
                  <Typography fontWeight={600}>{book.bookTitle}</Typography>
                  <Chip
                    label={`${book.borrowers.length} borrower${book.borrowers.length !== 1 ? 's' : ''}`}
                    color="primary"
                    size="small"
                  />
                  {book.borrowers.some((b) => isOverdue(b.borrowedAt)) && (
                    <Chip label="Overdue" color="error" size="small" />
                  )}
                </Box>
              </AccordionSummary>
              <AccordionDetails sx={{ p: 0 }}>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                      <TableCell>Customer</TableCell>
                      <TableCell>Borrowed At</TableCell>
                      <TableCell>Duration</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {book.borrowers.map((borrower) => {
                      const days = getDaysBorrowed(borrower.borrowedAt)
                      const overdue = isOverdue(borrower.borrowedAt)
                      return (
                        <TableRow
                          key={borrower.customerId}
                          sx={overdue ? { backgroundColor: '#FFF5F5' } : undefined}
                        >
                          <TableCell>{borrower.customerName}</TableCell>
                          <TableCell>
                            {new Date(borrower.borrowedAt).toLocaleDateString()}
                          </TableCell>
                          <TableCell>
                            <Typography
                              variant="body2"
                              sx={{ color: overdue ? '#DC3545' : 'inherit', fontWeight: overdue ? 600 : 400 }}
                            >
                              {days} day{days !== 1 ? 's' : ''}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={overdue ? 'Overdue' : 'Active'}
                              color={overdue ? 'error' : 'success'}
                              size="small"
                            />
                          </TableCell>
                          <TableCell>
                            <Button
                              size="small"
                              variant="outlined"
                              color="success"
                              onClick={() => handleReturn(book.bookId, borrower.customerId)}
                              disabled={returnMutation.isPending}
                            >
                              Return
                            </Button>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </AccordionDetails>
            </Accordion>
          ))}
          {!filteredSummary.length && (
            <EmptyState
              title="No active borrowings"
              description="Books that are currently borrowed will appear here"
            />
          )}
        </Box>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                <TableCell>Book</TableCell>
                <TableCell>Customer</TableCell>
                <TableCell>Borrowed At</TableCell>
                <TableCell>Duration</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredFlat.map((borrowing, index) => {
                const days = getDaysBorrowed(borrowing.borrowedAt)
                const overdue = isOverdue(borrowing.borrowedAt)
                return (
                  <TableRow
                    key={`${borrowing.bookId}-${borrowing.customerId}-${index}`}
                    sx={overdue ? { backgroundColor: '#FFF5F5' } : undefined}
                  >
                    <TableCell>{borrowing.bookTitle}</TableCell>
                    <TableCell>{borrowing.customerName}</TableCell>
                    <TableCell>{new Date(borrowing.borrowedAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Typography
                        variant="body2"
                        sx={{ color: overdue ? '#DC3545' : 'inherit', fontWeight: overdue ? 600 : 400 }}
                      >
                        {days}d
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={overdue ? 'Overdue' : 'Active'}
                        color={overdue ? 'error' : 'success'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        color="success"
                        onClick={() => handleReturn(borrowing.bookId, borrowing.customerId)}
                        disabled={returnMutation.isPending}
                      >
                        Return
                      </Button>
                    </TableCell>
                  </TableRow>
                )
              })}
              {!filteredFlat.length && (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <EmptyState title="No active borrowings" />
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <BorrowReturnDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        initialTab={dialogTab}
      />
    </Box>
  )
}

export default BorrowingOverview
