import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Button,
  Paper,
  Chip,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Divider,
} from '@mui/material'
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  CheckCircle as ReturnIcon,
} from '@mui/icons-material'
import { useBook, useDeleteBook } from '@/hooks/useBooks'
import { useBorrowingsByBook, useReturnBook } from '@/hooks/useBorrowings'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { useToast } from '@/context/ToastContext'

const BookDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: book, isLoading } = useBook(id ?? '')
  const { data: borrowings } = useBorrowingsByBook(id ?? '')
  const deleteMutation = useDeleteBook()
  const returnMutation = useReturnBook()

  const activeBorrowings = borrowings?.filter((b) => b.isActive) ?? []

  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync(id!)
      showSuccess('Book deleted successfully')
      navigate('/books')
    } catch {
      showError('Failed to delete book')
    }
  }

  const handleReturn = async (customerId: string) => {
    try {
      await returnMutation.mutateAsync({ bookId: id!, data: { customerId } })
      showSuccess('Book returned successfully')
    } catch {
      showError('Failed to return book')
    }
  }

  if (isLoading) return <LoadingSkeleton rows={4} columns={1} />
  if (!book) return <EmptyState title="Book not found" description="This book does not exist." />

  const availabilityPct = book.totalCopies > 0 ? (book.availableCopies / book.totalCopies) * 100 : 0

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/books')}>
          Back
        </Button>
        <Typography color="textSecondary">Books</Typography>
        <Typography color="textSecondary">/</Typography>
        <Typography fontWeight={600}>{book.title}</Typography>
      </Box>

      {/* Hero Card */}
      <Paper sx={{ p: 4, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 2 }}>
          <Box sx={{ flex: 1 }}>
            <Typography variant="h4" fontWeight={700} gutterBottom>
              {book.title}
            </Typography>
            <Typography variant="h6" color="textSecondary" gutterBottom>
              {book.authorName}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
              <Chip label={book.categoryName} color="primary" size="small" />
              <Chip
                label={book.availableCopies > 0 ? 'Available' : 'Fully Borrowed'}
                color={book.availableCopies > 0 ? 'success' : 'error'}
                size="small"
              />
            </Box>
            <Typography variant="body2" color="textSecondary" gutterBottom>
              ISBN:{' '}
              <Box component="code" sx={{ fontFamily: 'monospace', bgcolor: '#F5F7FA', px: 1, py: 0.25, borderRadius: 1 }}>
                {book.isbn}
              </Box>
            </Typography>
            <Typography variant="body2" color="textSecondary">
              Added {new Date(book.createdAt).toLocaleDateString()}
            </Typography>
          </Box>

          {/* Availability */}
          <Box sx={{ minWidth: 180 }}>
            <Typography variant="subtitle2" gutterBottom>
              Availability
            </Typography>
            <LinearProgress
              variant="determinate"
              value={availabilityPct}
              sx={{
                height: 10,
                borderRadius: 5,
                mb: 1,
                backgroundColor: '#E0E4E8',
                '& .MuiLinearProgress-bar': {
                  backgroundColor: book.availableCopies === 0 ? '#DC3545' : '#28A745',
                },
              }}
            />
            <Typography variant="body2" color="textSecondary">
              {book.availableCopies} of {book.totalCopies} copies available
            </Typography>
          </Box>
        </Box>

        <Divider sx={{ my: 3 }} />

        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<EditIcon />}
            onClick={() => navigate(`/books/${id}/edit`)}
          >
            Edit Book
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<DeleteIcon />}
            onClick={() => setDeleteOpen(true)}
          >
            Delete Book
          </Button>
        </Box>
      </Paper>

      {/* Currently Borrowed By */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Currently Borrowed By
        </Typography>
        {activeBorrowings.length === 0 ? (
          <EmptyState title="No active borrowings" description="This book is not currently borrowed by anyone." />
        ) : (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                  <TableCell>Customer</TableCell>
                  <TableCell>Borrowed At</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {activeBorrowings.map((b) => (
                  <TableRow key={b.id}>
                    <TableCell>{b.customerName}</TableCell>
                    <TableCell>{new Date(b.borrowedAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        color="success"
                        startIcon={<ReturnIcon />}
                        onClick={() => handleReturn(b.customerId)}
                        disabled={returnMutation.isPending}
                      >
                        Return
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>

      <ConfirmDialog
        open={deleteOpen}
        title="Delete Book"
        message={`Are you sure you want to delete "${book.title}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />
    </Box>
  )
}

export default BookDetail
