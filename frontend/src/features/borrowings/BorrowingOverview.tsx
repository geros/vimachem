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
  IconButton,
  Tooltip,
} from '@mui/material'
import {
  ExpandMore as ExpandMoreIcon,
  Add as AddIcon,
  CheckCircle as CheckCircleIcon,
  MenuBook as MenuBookIcon,
} from '@mui/icons-material'
import { useBorrowingSummary, useReturnBook } from '@/hooks/useBorrowings'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { useToast } from '@/context/ToastContext'
import BorrowReturnDialog from './BorrowReturnDialog'

type ViewMode = 'grouped' | 'flat'

const BorrowingOverview: React.FC = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('grouped')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogTab, setDialogTab] = useState<'borrow' | 'return'>('borrow')

  const { data: summary, isLoading } = useBorrowingSummary()
  const returnMutation = useReturnBook()
  const { showSuccess, showError } = useToast()

  const handleViewModeChange = (_: React.MouseEvent<HTMLElement>, newMode: ViewMode) => {
    if (newMode !== null) {
      setViewMode(newMode)
    }
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

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
          Borrowings
        </Typography>
        <Box display="flex" gap={2}>
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
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setDialogTab('borrow')
              setDialogOpen(true)
            }}
          >
            Borrow Book
          </Button>
        </Box>
      </Box>

      {viewMode === 'grouped' ? (
        <Box>
          {summary?.map((book) => (
            <Accordion key={book.bookId}>
              <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                <Box display="flex" alignItems="center" gap={2}>
                  <MenuBookIcon />
                  <Typography variant="h6">{book.bookTitle}</Typography>
                  <Chip
                    label={`${book.borrowers.length} borrower(s)`}
                    color="primary"
                    size="small"
                  />
                </Box>
              </AccordionSummary>
              <AccordionDetails>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Customer</TableCell>
                      <TableCell>Borrowed At</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {book.borrowers.map((borrower) => (
                      <TableRow key={borrower.customerId}>
                        <TableCell>{borrower.customerName}</TableCell>
                        <TableCell>
                          {new Date(borrower.borrowedAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <Tooltip title="Return Book">
                            <IconButton
                              color="success"
                              onClick={() =>
                                handleReturn(book.bookId, borrower.customerId)
                              }
                              disabled={returnMutation.isPending}
                            >
                              <CheckCircleIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </AccordionDetails>
            </Accordion>
          ))}
          {!summary?.length && (
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
                <TableCell>Status</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {flatBorrowings.map((borrowing, index) => (
                <TableRow key={`${borrowing.bookId}-${borrowing.customerId}-${index}`}>
                  <TableCell>{borrowing.bookTitle}</TableCell>
                  <TableCell>{borrowing.customerName}</TableCell>
                  <TableCell>
                    {new Date(borrowing.borrowedAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <Chip label="Active" color="success" size="small" />
                  </TableCell>
                  <TableCell>
                    <Tooltip title="Return Book">
                      <IconButton
                        color="success"
                        onClick={() =>
                          handleReturn(borrowing.bookId, borrowing.customerId)
                        }
                        disabled={returnMutation.isPending}
                      >
                        <CheckCircleIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {!flatBorrowings.length && (
                <TableRow>
                  <TableCell colSpan={5} align="center">
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
