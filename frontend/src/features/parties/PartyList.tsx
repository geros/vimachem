import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Avatar,
  Box,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material'
import { useParties, useDeleteParty } from '@/hooks/useParties'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { RoleBadge } from '@/components/shared/RoleBadge'
import { Pagination } from '@/components/shared/Pagination'
import { useToast } from '@/context/ToastContext'
import type { Party } from '@/types/party'

const AVATAR_COLORS = ['#0077C8', '#00A3B5', '#28A745', '#F5A623', '#DC3545', '#6f42c1']

const getAvatarColor = (firstName: string, lastName: string) => {
  const idx = (firstName.charCodeAt(0) + lastName.charCodeAt(0)) % AVATAR_COLORS.length
  return AVATAR_COLORS[idx]
}

const PartyList: React.FC = () => {
  const navigate = useNavigate()
  const { data: parties, isLoading } = useParties()
  const deleteMutation = useDeleteParty()
  const { showSuccess, showError } = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [roleFilter, setRoleFilter] = useState<'all' | 'Author' | 'Customer'>('all')
  const [deleteTarget, setDeleteTarget] = useState<Party | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  const filteredParties =
    parties?.filter((party) => {
      const matchesSearch =
        party.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        party.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        party.email.toLowerCase().includes(searchTerm.toLowerCase())
      const matchesRole = roleFilter === 'all' || party.roles.includes(roleFilter)
      return matchesSearch && matchesRole
    }) ?? []

  const paginatedParties = filteredParties.slice((page - 1) * pageSize, page * pageSize)

  const handleDelete = async () => {
    if (!deleteTarget) return
    try {
      await deleteMutation.mutateAsync(deleteTarget.id)
      showSuccess(`Party "${deleteTarget.firstName} ${deleteTarget.lastName}" deleted successfully`)
      setDeleteTarget(null)
    } catch {
      showError('Failed to delete party')
    }
  }

  if (isLoading) return <LoadingSkeleton rows={5} columns={5} />

  if (!parties?.length) {
    return (
      <EmptyState
        title="No parties found"
        description="Get started by adding your first party"
        actionLabel="Add Party"
        onAction={() => navigate('/parties/new')}
      />
    )
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 3, flexWrap: 'wrap' }}>
        <TextField
          placeholder="Search parties..."
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPage(1) }}
          size="small"
          sx={{ width: 280 }}
        />
        <ToggleButtonGroup
          value={roleFilter}
          exclusive
          onChange={(_, v) => { if (v) { setRoleFilter(v); setPage(1) } }}
          size="small"
        >
          <ToggleButton value="all">All</ToggleButton>
          <ToggleButton value="Author">Author</ToggleButton>
          <ToggleButton value="Customer">Customer</ToggleButton>
        </ToggleButtonGroup>
        <Box sx={{ flex: 1 }} />
        <Typography variant="body2" color="textSecondary">
          {filteredParties.length} part{filteredParties.length !== 1 ? 'ies' : 'y'}
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate('/parties/new')}
        >
          Add Party
        </Button>
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Roles</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {paginatedParties.map((party) => (
              <TableRow
                key={party.id}
                hover
                sx={{ cursor: 'pointer' }}
                onClick={() => navigate(`/parties/${party.id}`)}
              >
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    <Avatar
                      sx={{
                        width: 36,
                        height: 36,
                        fontSize: '0.8rem',
                        fontWeight: 700,
                        bgcolor: getAvatarColor(party.firstName, party.lastName),
                      }}
                    >
                      {party.firstName[0]}{party.lastName[0]}
                    </Avatar>
                    <Typography fontWeight={500}>
                      {party.firstName} {party.lastName}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>{party.email}</TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', gap: 0.5 }}>
                    {party.roles.map((role) => (
                      <RoleBadge key={role} role={role} />
                    ))}
                  </Box>
                </TableCell>
                <TableCell>{new Date(party.createdAt).toLocaleDateString()}</TableCell>
                <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                  <IconButton size="small" onClick={() => navigate(`/parties/${party.id}`)}>
                    <VisibilityIcon />
                  </IconButton>
                  <IconButton size="small" onClick={() => navigate(`/parties/${party.id}/edit`)}>
                    <EditIcon />
                  </IconButton>
                  <IconButton size="small" color="error" onClick={() => setDeleteTarget(party)}>
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Pagination
        count={filteredParties.length}
        page={page}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={(ps) => { setPageSize(ps); setPage(1) }}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete Party"
        message={`Are you sure you want to delete "${deleteTarget?.firstName} ${deleteTarget?.lastName}"? This action cannot be undone.`}
        confirmLabel="Delete"
        destructive
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  )
}

export default PartyList
