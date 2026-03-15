import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Avatar,
  Box,
  Button,
  Chip,
  LinearProgress,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Typography,
} from '@mui/material'
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Email as EmailIcon,
  CalendarToday as CalendarIcon,
} from '@mui/icons-material'
import { useParty, useDeleteParty } from '@/hooks/useParties'
import { useBooks } from '@/hooks/useBooks'
import { useBorrowingsByCustomer } from '@/hooks/useBorrowings'
import { RoleBadge } from '@/components/shared/RoleBadge'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { EmptyState } from '@/components/shared/EmptyState'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { useToast } from '@/context/ToastContext'

const AVATAR_COLORS = ['#0077C8', '#00A3B5', '#28A745', '#F5A623', '#DC3545', '#6f42c1']

const getAvatarColor = (firstName: string, lastName: string) => {
  const idx = (firstName.charCodeAt(0) + lastName.charCodeAt(0)) % AVATAR_COLORS.length
  return AVATAR_COLORS[idx]
}

const PartyDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [activeTab, setActiveTab] = useState(0)

  const { data: party, isLoading } = useParty(id ?? '')
  const { data: allBooks } = useBooks()
  const { data: borrowingHistory } = useBorrowingsByCustomer(id ?? '')
  const deleteMutation = useDeleteParty()

  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync(id!)
      showSuccess('Party deleted successfully')
      navigate('/parties')
    } catch {
      showError('Failed to delete party')
    }
  }

  if (isLoading) return <LoadingSkeleton rows={4} columns={1} />
  if (!party) return <EmptyState title="Party not found" description="This party does not exist." />

  const isAuthor = party.roles.includes('Author')
  const isCustomer = party.roles.includes('Customer')
  const authoredBooks = allBooks?.filter((b) => b.authorId === id) ?? []
  const activeborrowings = borrowingHistory?.filter((b) => b.isActive) ?? []
  const pastBorrowings = borrowingHistory?.filter((b) => !b.isActive) ?? []

  const tabs: string[] = []
  if (isAuthor) tabs.push('Authored Books')
  if (isCustomer) tabs.push('Borrowing History')
  tabs.push('Activity')

  return (
    <Box>
      {/* Breadcrumb */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/parties')}>
          Back
        </Button>
        <Typography color="textSecondary">Parties</Typography>
        <Typography color="textSecondary">/</Typography>
        <Typography fontWeight={600}>{party.firstName} {party.lastName}</Typography>
      </Box>

      {/* Hero Card */}
      <Paper sx={{ p: 4, mb: 3 }}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', alignItems: 'flex-start' }}>
          <Avatar
            sx={{
              width: 80,
              height: 80,
              fontSize: '1.75rem',
              fontWeight: 700,
              bgcolor: getAvatarColor(party.firstName, party.lastName),
            }}
          >
            {party.firstName[0]}{party.lastName[0]}
          </Avatar>
          <Box sx={{ flex: 1 }}>
            <Typography variant="h4" fontWeight={700} gutterBottom>
              {party.firstName} {party.lastName}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
              {party.roles.map((role) => (
                <RoleBadge key={role} role={role} />
              ))}
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
              <EmailIcon fontSize="small" color="action" />
              <Typography variant="body2" color="textSecondary">{party.email}</Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <CalendarIcon fontSize="small" color="action" />
              <Typography variant="body2" color="textSecondary">
                Member since {new Date(party.createdAt).toLocaleDateString()}
              </Typography>
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 2, flexShrink: 0 }}>
            <Button
              variant="outlined"
              startIcon={<EditIcon />}
              onClick={() => navigate(`/parties/${id}/edit`)}
            >
              Edit
            </Button>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteIcon />}
              onClick={() => setDeleteOpen(true)}
            >
              Delete
            </Button>
          </Box>
        </Box>
      </Paper>

      {/* Tabs */}
      <Paper>
        <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
          {tabs.map((tab) => (
            <Tab key={tab} label={tab} />
          ))}
        </Tabs>

        <Box sx={{ p: 3 }}>
          {/* Authored Books */}
          {tabs[activeTab] === 'Authored Books' && (
            authoredBooks.length === 0 ? (
              <EmptyState title="No authored books" description="No books are linked to this author yet." />
            ) : (
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                      <TableCell>Title</TableCell>
                      <TableCell>ISBN</TableCell>
                      <TableCell>Category</TableCell>
                      <TableCell>Availability</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {authoredBooks.map((book) => (
                      <TableRow
                        key={book.id}
                        hover
                        sx={{ cursor: 'pointer' }}
                        onClick={() => navigate(`/books/${book.id}`)}
                      >
                        <TableCell>
                          <Typography fontWeight={500}>{book.title}</Typography>
                        </TableCell>
                        <TableCell>
                          <Box component="code" sx={{ fontFamily: 'monospace', fontSize: '0.85em' }}>
                            {book.isbn}
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Chip label={book.categoryName} size="small" variant="outlined" />
                        </TableCell>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <LinearProgress
                              variant="determinate"
                              value={book.totalCopies > 0 ? (book.availableCopies / book.totalCopies) * 100 : 0}
                              sx={{
                                width: 60,
                                height: 6,
                                borderRadius: 3,
                                backgroundColor: '#E0E4E8',
                                '& .MuiLinearProgress-bar': {
                                  backgroundColor: book.availableCopies === 0 ? '#DC3545' : '#28A745',
                                },
                              }}
                            />
                            <Typography variant="caption" color="textSecondary">
                              {book.availableCopies}/{book.totalCopies}
                            </Typography>
                          </Box>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )
          )}

          {/* Borrowing History */}
          {tabs[activeTab] === 'Borrowing History' && (
            borrowingHistory?.length === 0 ? (
              <EmptyState title="No borrowing history" description="This customer has not borrowed any books yet." />
            ) : (
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                      <TableCell>Book</TableCell>
                      <TableCell>Borrowed At</TableCell>
                      <TableCell>Returned At</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {[...activeborrowings, ...pastBorrowings].map((b) => (
                      <TableRow key={b.id}>
                        <TableCell>{b.bookTitle}</TableCell>
                        <TableCell>{new Date(b.borrowedAt).toLocaleDateString()}</TableCell>
                        <TableCell>
                          {b.returnedAt ? new Date(b.returnedAt).toLocaleDateString() : '—'}
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={b.isActive ? 'Active' : 'Returned'}
                            color={b.isActive ? 'success' : 'default'}
                            size="small"
                          />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )
          )}

          {/* Activity */}
          {tabs[activeTab] === 'Activity' && (
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <Typography color="textSecondary" gutterBottom>
                View the full audit event log for this party in the Audit section.
              </Typography>
              <Button variant="outlined" onClick={() => navigate('/audit')}>
                Go to Audit Log
              </Button>
            </Box>
          )}
        </Box>
      </Paper>

      <ConfirmDialog
        open={deleteOpen}
        title="Delete Party"
        message={`Are you sure you want to delete "${party.firstName} ${party.lastName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />
    </Box>
  )
}

export default PartyDetail
