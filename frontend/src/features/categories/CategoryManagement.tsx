import { useState } from 'react'
import {
  Box,
  Typography,
  Button,
  TextField,
  Paper,
  IconButton,
  Grid,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Tooltip,
} from '@mui/material'
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Folder as FolderIcon,
  MenuBook as BookIcon,
} from '@mui/icons-material'
import { useCategories, useCreateCategory, useUpdateCategory, useDeleteCategory } from '@/hooks/useCategories'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { useToast } from '@/context/ToastContext'
import type { Category } from '@/types/category'

const CATEGORY_COLORS = [
  { bg: '#EFF6FF', icon: '#3B82F6', border: '#BFDBFE' },
  { bg: '#F5F3FF', icon: '#7C3AED', border: '#DDD6FE' },
  { bg: '#F0FDF4', icon: '#16A34A', border: '#BBF7D0' },
  { bg: '#FFF7ED', icon: '#EA580C', border: '#FED7AA' },
  { bg: '#FFF1F2', icon: '#E11D48', border: '#FECDD3' },
]

const getCategoryColor = (index: number) => CATEGORY_COLORS[index % CATEGORY_COLORS.length]

const CategoryManagement: React.FC = () => {
  const { data: categories, isLoading } = useCategories()
  const createMutation = useCreateCategory()
  const updateMutation = useUpdateCategory()
  const deleteMutation = useDeleteCategory()
  const { showSuccess, showError } = useToast()

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [categoryName, setCategoryName] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null)

  const handleOpenDialog = (category?: Category) => {
    if (category) {
      setEditingCategory(category)
      setCategoryName(category.name)
    } else {
      setEditingCategory(null)
      setCategoryName('')
    }
    setDialogOpen(true)
  }

  const handleCloseDialog = () => {
    setDialogOpen(false)
    setEditingCategory(null)
    setCategoryName('')
  }

  const handleSave = async () => {
    if (!categoryName.trim()) return
    try {
      if (editingCategory) {
        await updateMutation.mutateAsync({ id: editingCategory.id, data: { name: categoryName } })
        showSuccess('Category updated successfully')
      } else {
        await createMutation.mutateAsync({ name: categoryName })
        showSuccess('Category created successfully')
      }
      handleCloseDialog()
    } catch {
      showError(editingCategory ? 'Failed to update category' : 'Failed to create category')
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      await deleteMutation.mutateAsync(deleteTarget.id)
      showSuccess(`Category "${deleteTarget.name}" deleted successfully`)
      setDeleteTarget(null)
    } catch {
      showError('Failed to delete category')
    }
  }

  if (isLoading) return <LoadingSkeleton rows={3} columns={3} />

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Box>
          <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
            Categories
          </Typography>
          <Typography variant="body2" color="textSecondary">
            {categories?.length ?? 0} categor{categories?.length !== 1 ? 'ies' : 'y'}
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => handleOpenDialog()}>
          Add Category
        </Button>
      </Box>

      {!categories?.length ? (
        <EmptyState
          title="No categories found"
          description="Get started by adding your first category"
          actionLabel="Add Category"
          onAction={() => handleOpenDialog()}
        />
      ) : (
        <Grid container spacing={3}>
          {categories.map((category, index) => {
            const color = getCategoryColor(index)
            const hasBooks = category.bookCount > 0
            return (
              <Grid key={category.id} size={{ xs: 12, sm: 6, md: 4 }}>
                <Paper
                  sx={{
                    p: 3,
                    border: `1px solid ${color.border}`,
                    backgroundColor: color.bg,
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                  }}
                >
                  {/* Card Header */}
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    <Box
                      sx={{
                        p: 1.5,
                        borderRadius: 2,
                        backgroundColor: `${color.icon}20`,
                        display: 'flex',
                        alignItems: 'center',
                      }}
                    >
                      <FolderIcon sx={{ color: color.icon, fontSize: 28 }} />
                    </Box>
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="h6" fontWeight={600}>
                        {category.name}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                        <BookIcon sx={{ fontSize: 14, color: 'textSecondary' }} />
                        <Typography variant="body2" color="textSecondary">
                          {category.bookCount} book{category.bookCount !== 1 ? 's' : ''}
                        </Typography>
                      </Box>
                    </Box>
                  </Box>

                  <Box sx={{ flex: 1 }} />

                  {/* Card Footer */}
                  <Box
                    sx={{
                      display: 'flex',
                      gap: 1,
                      pt: 2,
                      borderTop: `1px solid ${color.border}`,
                      mt: 2,
                    }}
                  >
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<EditIcon />}
                      onClick={() => handleOpenDialog(category)}
                      sx={{ borderColor: color.icon, color: color.icon, flex: 1 }}
                    >
                      Edit
                    </Button>
                    <Tooltip
                      title={hasBooks ? 'Cannot delete a category that has books' : ''}
                    >
                      <span style={{ flex: 1 }}>
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<DeleteIcon />}
                          color="error"
                          fullWidth
                          disabled={hasBooks}
                          onClick={() => setDeleteTarget(category)}
                        >
                          Delete
                        </Button>
                      </span>
                    </Tooltip>
                  </Box>
                </Paper>
              </Grid>
            )
          })}
        </Grid>
      )}

      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>{editingCategory ? 'Edit Category' : 'Add Category'}</DialogTitle>
        <DialogContent>
          <TextField
            label="Category Name"
            value={categoryName}
            onChange={(e) => setCategoryName(e.target.value)}
            fullWidth
            sx={{ mt: 1 }}
            autoFocus
            onKeyDown={(e) => { if (e.key === 'Enter') handleSave() }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            onClick={handleSave}
            variant="contained"
            disabled={!categoryName.trim() || createMutation.isPending || updateMutation.isPending}
          >
            {editingCategory ? 'Update' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Category"
        message={`Are you sure you want to delete "${deleteTarget?.name}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  )
}

export default CategoryManagement
