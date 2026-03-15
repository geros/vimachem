import React, { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Skeleton,
  Button,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  LinearProgress,
} from '@mui/material'
import {
  MenuBook as BookIcon,
  People as PeopleIcon,
  SwapHoriz as BorrowingIcon,
  CheckCircle as AvailableIcon,
  PersonAdd as AddPartyIcon,
  Add as AddBookIcon,
  AssignmentReturn as BorrowIcon,
} from '@mui/icons-material'
import { useParties } from '@/hooks/useParties'
import { useBooks } from '@/hooks/useBooks'
import { useBorrowingSummary } from '@/hooks/useBorrowings'
import { useToast } from '@/context/ToastContext'

interface StatCardProps {
  title: string
  value: number | string
  icon: React.ReactNode
  color: string
  loading?: boolean
}

const StatCard: React.FC<StatCardProps> = ({ title, value, icon, color, loading }) => (
  <Card>
    <CardContent>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Box>
          <Typography color="textSecondary" variant="body2" gutterBottom>
            {title}
          </Typography>
          {loading ? (
            <Skeleton variant="text" width={60} height={40} />
          ) : (
            <Typography variant="h4" sx={{ color }}>
              {value}
            </Typography>
          )}
        </Box>
        <Box sx={{ p: 1.5, borderRadius: 2, backgroundColor: `${color}20`, color }}>
          {icon}
        </Box>
      </Box>
    </CardContent>
  </Card>
)

const DashboardPage: React.FC = () => {
  const navigate = useNavigate()
  const { data: parties, isLoading: partiesLoading, error: partiesError } = useParties()
  const { data: books, isLoading: booksLoading, error: booksError } = useBooks()
  const { data: borrowings, isLoading: borrowingsLoading, error: borrowingsError } = useBorrowingSummary()
  const { showError } = useToast()

  useEffect(() => {
    if (partiesError) showError('Failed to load parties')
    if (booksError) showError('Failed to load books')
    if (borrowingsError) showError('Failed to load borrowings')
  }, [partiesError, booksError, borrowingsError, showError])

  const totalBooks = books?.length ?? 0
  const totalParties = parties?.length ?? 0
  const activeBorrowings = borrowings?.reduce((sum, b) => sum + b.borrowers.length, 0) ?? 0
  const availableBooks = books?.reduce((sum, b) => sum + b.availableCopies, 0) ?? 0

  // Recently borrowed: take the first 5 books with active borrowers
  const recentlyBorrowed = borrowings?.slice(0, 5) ?? []

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ color: '#003B6F', fontWeight: 600 }}>
        Dashboard
      </Typography>
      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Welcome to the Library Management System. Here is an overview of your library.
      </Typography>

      {/* Stat Cards */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard title="Total Books" value={totalBooks} icon={<BookIcon />} color="#0077C8" loading={booksLoading} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard title="Total Parties" value={totalParties} icon={<PeopleIcon />} color="#00A3B5" loading={partiesLoading} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard title="Active Borrowings" value={activeBorrowings} icon={<BorrowingIcon />} color="#F5A623" loading={borrowingsLoading} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard title="Available Books" value={availableBooks} icon={<AvailableIcon />} color="#28A745" loading={booksLoading} />
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        {/* Recently Borrowed */}
        <Grid size={{ xs: 12, md: 8 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Currently Borrowed Books
            </Typography>
            {borrowingsLoading ? (
              <Skeleton variant="rectangular" height={200} />
            ) : recentlyBorrowed.length === 0 ? (
              <Typography color="textSecondary" sx={{ py: 4, textAlign: 'center' }}>
                No books currently borrowed
              </Typography>
            ) : (
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                      <TableCell>Book</TableCell>
                      <TableCell>Borrowers</TableCell>
                      <TableCell>Availability</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {recentlyBorrowed.map((item) => {
                      const book = books?.find((b) => b.id === item.bookId)
                      const availPct = book ? (book.availableCopies / book.totalCopies) * 100 : 0
                      return (
                        <TableRow
                          key={item.bookId}
                          hover
                          sx={{ cursor: 'pointer' }}
                          onClick={() => navigate(`/books/${item.bookId}`)}
                        >
                          <TableCell>
                            <Typography fontWeight={500}>{item.bookTitle}</Typography>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={`${item.borrowers.length} borrower${item.borrowers.length !== 1 ? 's' : ''}`}
                              size="small"
                              color="warning"
                            />
                          </TableCell>
                          <TableCell>
                            {book && (
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <LinearProgress
                                  variant="determinate"
                                  value={availPct}
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
                            )}
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
            <Box sx={{ mt: 2 }}>
              <Button size="small" onClick={() => navigate('/borrowings')}>
                View all borrowings →
              </Button>
            </Box>
          </Paper>
        </Grid>

        {/* Quick Actions */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" fontWeight={600} gutterBottom>
              Quick Actions
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
              <Button
                variant="outlined"
                fullWidth
                startIcon={<AddPartyIcon />}
                onClick={() => navigate('/parties/new')}
                sx={{ justifyContent: 'flex-start', py: 1.5 }}
              >
                Add New Party
              </Button>
              <Button
                variant="outlined"
                fullWidth
                startIcon={<AddBookIcon />}
                onClick={() => navigate('/books/new')}
                sx={{ justifyContent: 'flex-start', py: 1.5 }}
              >
                Add New Book
              </Button>
              <Button
                variant="outlined"
                fullWidth
                startIcon={<BorrowIcon />}
                onClick={() => navigate('/borrowings')}
                sx={{ justifyContent: 'flex-start', py: 1.5 }}
              >
                Manage Borrowings
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

export default DashboardPage
