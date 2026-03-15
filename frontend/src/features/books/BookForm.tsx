import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Typography,
  TextField,
  Button,
  Paper,
  MenuItem,
  Autocomplete,
} from '@mui/material'
import { ArrowBack as ArrowBackIcon } from '@mui/icons-material'
import { useBook, useCreateBook, useUpdateBook } from '@/hooks/useBooks'
import { useCategories } from '@/hooks/useCategories'
import { useParties } from '@/hooks/useParties'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { useToast } from '@/context/ToastContext'
import { RoleType } from '@/types/party'

const BookForm: React.FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const isEditing = !!id

  const { data: book, isLoading: bookLoading } = useBook(id || '')
  const { data: categories } = useCategories()
  const { data: parties } = useParties()
  const createMutation = useCreateBook()
  const updateMutation = useUpdateBook()

  const authors = parties?.filter((p) => p.roles.includes('Author')) ?? []

  const [formData, setFormData] = useState({
    title: '',
    isbn: '',
    authorId: '',
    categoryId: '',
    totalCopies: 1,
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (book) {
      setFormData({
        title: book.title,
        isbn: book.isbn,
        authorId: book.authorId,
        categoryId: book.categoryId,
        totalCopies: book.totalCopies,
      })
    }
  }, [book])

  const validate = () => {
    const newErrors: Record<string, string> = {}
    if (!formData.title.trim()) newErrors.title = 'Title is required'
    if (!formData.isbn.trim()) {
      newErrors.isbn = 'ISBN is required'
    } else if (!/^\d{10}(\d{3})?$/.test(formData.isbn.replace(/-/g, ''))) {
      newErrors.isbn = 'ISBN must be 10 or 13 digits'
    }
    if (!formData.authorId) newErrors.authorId = 'Author is required'
    if (!formData.categoryId) newErrors.categoryId = 'Category is required'
    if (formData.totalCopies < 1) newErrors.totalCopies = 'Must be at least 1'
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          id: id!,
          data: {
            title: formData.title,
            categoryId: formData.categoryId,
            totalCopies: formData.totalCopies,
          },
        })
        showSuccess('Book updated successfully')
      } else {
        await createMutation.mutateAsync(formData)
        showSuccess('Book created successfully')
      }
      navigate('/books')
    } catch {
      showError(isEditing ? 'Failed to update book' : 'Failed to create book')
    }
  }

  if (isEditing && bookLoading) {
    return <LoadingSkeleton rows={3} columns={1} />
  }

  const selectedAuthor = authors.find((a) => a.id === formData.authorId)

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/books')}>
          Back
        </Button>
        <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
          {isEditing ? 'Edit Book' : 'Add New Book'}
        </Typography>
      </Box>

      <Paper sx={{ p: 4, maxWidth: 600 }}>
        <form onSubmit={handleSubmit}>
          <TextField
            label="Title"
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            error={!!errors.title}
            helperText={errors.title}
            fullWidth
            sx={{ mb: 3 }}
          />

          <TextField
            label="ISBN"
            value={formData.isbn}
            onChange={(e) => setFormData({ ...formData, isbn: e.target.value })}
            error={!!errors.isbn}
            helperText={errors.isbn}
            disabled={isEditing}
            fullWidth
            sx={{ mb: 3 }}
          />

          <Autocomplete
            options={authors}
            getOptionLabel={(option) => `${option.firstName} ${option.lastName}`}
            value={selectedAuthor || null}
            onChange={(_, newValue) => {
              setFormData({ ...formData, authorId: newValue?.id || '' })
            }}
            disabled={isEditing}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Author"
                error={!!errors.authorId}
                helperText={errors.authorId}
                sx={{ mb: 3 }}
              />
            )}
          />

          <TextField
            select
            label="Category"
            value={formData.categoryId}
            onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
            error={!!errors.categoryId}
            helperText={errors.categoryId}
            fullWidth
            sx={{ mb: 3 }}
          >
            {categories?.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label="Total Copies"
            type="number"
            value={formData.totalCopies}
            onChange={(e) =>
              setFormData({ ...formData, totalCopies: parseInt(e.target.value) || 0 })
            }
            error={!!errors.totalCopies}
            helperText={errors.totalCopies}
            fullWidth
            sx={{ mb: 3 }}
          />

          <Box sx={{ display: 'flex', gap: 2, mt: 4 }}>
            <Button variant="outlined" onClick={() => navigate('/books')}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {isEditing ? 'Update' : 'Create'}
            </Button>
          </Box>
        </form>
      </Paper>
    </Box>
  )
}

export default BookForm
