import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Button,
  TextField,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  LinearProgress,
  MenuItem,
} from '@mui/material'
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material'
import { useBooks, useDeleteBook } from '@/hooks/useBooks'
import { useCategories } from '@/hooks/useCategories'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { Pagination } from '@/components/shared/Pagination'
import { useToast } from '@/context/ToastContext'
import type { Book } from '@/types/book'

const BookList: React.FC = () => {
  const navigate = useNavigate()
  const { data: books, isLoading } = useBooks()
  const { data: categories } = useCategories()
  const deleteMutation = useDeleteBook()
  const { showSuccess, showError } = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<Book | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  const filteredBooks =
    books?.filter((book) => {
      const matchesSearch =
        book.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        book.authorName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        book.isbn.toLowerCase().includes(searchTerm.toLowerCase())
      const matchesCategory = !categoryFilter || book.categoryId === categoryFilter
      return matchesSearch && matchesCategory
    }) ?? []

  const paginatedBooks = filteredBooks.slice((page - 1) * pageSize, page * pageSize)

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

  if (isLoading) return <LoadingSkeleton rows={5} columns={6} />

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
      <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 3, flexWrap: 'wrap' }}>
        <TextField
          placeholder="Search by title, author or ISBN..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPage(1) }}
          size="small"
          sx={{ width: 300 }}
        />
        <TextField
          select
          label="Category"
          value={categoryFilter}
          onChange={(e) => { setCategoryFilter(e.target.value); setPage(1) }}
          size="small"
          sx={{ width: 180 }}
        >
          <MenuItem value="">All Categories</MenuItem>
          {categories?.map((c) => (
            <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
          ))}
        </TextField>
        <Box sx={{ flex: 1 }} />
        <Typography variant="body2" color="textSecondary">
          {filteredBooks.length} book{filteredBooks.length !== 1 ? 's' : ''}
        </Typography>
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
              <TableCell>ISBN</TableCell>
              <TableCell>Author</TableCell>
              <TableCell>Category</TableCell>
              <TableCell>Availability</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {paginatedBooks.map((book) => (
              <TableRow
                key={book.id}
                hover
                sx={{
                  cursor: 'pointer',
                  ...(book.availableCopies === 0 && {
                    borderLeft: '3px solid #DC3545',
                    backgroundColor: '#FFF5F5',
                  }),
                }}
                onClick={() => navigate(`/books/${book.id}`)}
              >
                <TableCell>
                  <Typography fontWeight={500}>{book.title}</Typography>
                </TableCell>
                <TableCell>
                  <Box component="code" sx={{ fontFamily: 'monospace', fontSize: '0.85em', color: '#6B7280' }}>
                    {book.isbn}
                  </Box>
                </TableCell>
                <TableCell>{book.authorName}</TableCell>
                <TableCell>
                  <Chip label={book.categoryName} size="small" variant="outlined" />
                </TableCell>
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
                          backgroundColor: book.availableCopies === 0 ? '#DC3545' : '#28A745',
                        },
                      }}
                    />
                    <Typography variant="body2" color="textSecondary">
                      {book.availableCopies}/{book.totalCopies}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                  <IconButton size="small" onClick={() => navigate(`/books/${book.id}`)}>
                    <VisibilityIcon />
                  </IconButton>
                  <IconButton size="small" onClick={() => navigate(`/books/${book.id}/edit`)}>
                    <EditIcon />
                  </IconButton>
                  <IconButton size="small" color="error" onClick={() => setDeleteTarget(book)}>
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Pagination
        count={filteredBooks.length}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={(ps) => { setPageSize(ps); setPage(1) }}
      />

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
