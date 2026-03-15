import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Button,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  LinearProgress,
} from '@mui/material'
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material'
import { useBooks, useDeleteBook } from '@/hooks/useBooks'
import { useCategories } from '@/hooks/useCategories'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { useToast } from '@/context/ToastContext'
import type { Book } from '@/types/book'

const BookList: React.FC = () => {
  const navigate = useNavigate()
  const { data: books, isLoading } = useBooks()
  const { data: categories } = useCategories()
  const deleteMutation = useDeleteBook()
  const { showSuccess, showError } = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<Book | null>(null)

  const filteredBooks =
    books?.filter(
      (book) =>
        book.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        book.authorName.toLowerCase().includes(searchTerm.toLowerCase())
    ) ?? []

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      await deleteMutation.mutateAsync(deleteTarget.id)
      showSuccess(`Book "${deleteTarget.title}" deleted successfully`)
      setDeleteTarget(null)
    } catch {
      showError('Failed to delete book')
    }
  }

  const getAvailabilityPercentage = (book: Book) => {
    return book.totalCopies > 0 ? (book.availableCopies / book.totalCopies) * 100 : 0
  }

  if (isLoading) {
    return <LoadingSkeleton rows={5} columns={6} />
  }

  if (!books?.length) {
    return (
      <EmptyState
        title="No books found"
        description="Get started by adding your first book"
        actionLabel="Add Book"
        onAction={() => navigate('/books/new')}
      />
    )
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <TextField
          placeholder="Search books..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ width: 300 }}
        />
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate('/books/new')}
        >
          Add Book
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
              <TableCell>Title</TableCell>
              <TableCell>Author</TableCell>
              <TableCell>Category</TableCell>
              <TableCell>Availability</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredBooks.map((book) => (
              <TableRow
                key={book.id}
                hover
                sx={{ cursor: 'pointer' }}
                onClick={() => navigate(`/books/${book.id}`)}
              >
                <TableCell>
                  <Typography fontWeight={500}>{book.title}</Typography>
                  <Typography variant="caption" color="textSecondary">
                    ISBN: {book.isbn}
                  </Typography>
                </TableCell>
                <TableCell>{book.authorName}</TableCell>
                <TableCell>{book.categoryName}</TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <LinearProgress
                      variant="determinate"
                      value={getAvailabilityPercentage(book)}
                      sx={{
                        width: 60,
                        height: 8,
                        borderRadius: 4,
                        backgroundColor: '#E0E4E8',
                        '& .MuiLinearProgress-bar': {
                          backgroundColor:
                            book.availableCopies === 0 ? '#DC3545' : '#28A745',
                        },
                      }}
                    />
                    <Typography variant="body2" color="textSecondary">
                      {book.availableCopies}/{book.totalCopies}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation()
                      navigate(`/books/${book.id}/edit`)
                    }}
                  >
                    <EditIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    color="error"
                    onClick={(e) => {
                      e.stopPropagation()
                      setDeleteTarget(book)
                    }}
                  >
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Book"
        message={`Are you sure you want to delete "${deleteTarget?.title}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  )
}

export default BookList
