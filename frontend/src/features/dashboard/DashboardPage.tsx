import { useEffect } from 'react'
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  Skeleton,
} from '@mui/material'
import {
  MenuBook as BookIcon,
  People as PeopleIcon,
  SwapHoriz as BorrowingIcon,
  CheckCircle as AvailableIcon,
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
        <Box
          sx={{
            p: 1.5,
            borderRadius: 2,
            backgroundColor: `${color}20`,
            color,
          }}
        >
          {icon}
        </Box>
      </Box>
    </CardContent>
  </Card>
)

const DashboardPage: React.FC = () => {
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
  const activeBorrowings =
    borrowings?.reduce((sum, book) => sum + book.borrowers.length, 0) ?? 0
  const availableBooks =
    books?.reduce((sum, book) => sum + book.availableCopies, 0) ?? 0

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ color: '#003B6F', fontWeight: 600 }}>
        Dashboard
      </Typography>
      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Welcome to the Library Management System. Here is an overview of your library.
      </Typography>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            title="Total Books"
            value={totalBooks}
            icon={<BookIcon />}
            color="#0077C8"
            loading={booksLoading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            title="Total Parties"
            value={totalParties}
            icon={<PeopleIcon />}
            color="#00A3B5"
            loading={partiesLoading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            title="Active Borrowings"
            value={activeBorrowings}
            icon={<BorrowingIcon />}
            color="#F5A623"
            loading={borrowingsLoading}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <StatCard
            title="Available Books"
            value={availableBooks}
            icon={<AvailableIcon />}
            color="#28A745"
            loading={booksLoading}
          />
        </Grid>
      </Grid>
    </Box>
  )
}

export default DashboardPage
