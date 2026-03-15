import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
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
} from '@mui/material'
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material'
import { useParties, useDeleteParty } from '@/hooks/useParties'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { RoleBadge } from '@/components/shared/RoleBadge'
import { useToast } from '@/context/ToastContext'
import type { Party } from '@/types/party'

interface PartyListProps {
  onEdit?: (party: Party) => void
}

const PartyList: React.FC<PartyListProps> = ({ onEdit }) => {
  const navigate = useNavigate()
  const { data: parties, isLoading } = useParties()
  const deleteMutation = useDeleteParty()
  const { showSuccess, showError } = useToast()
  const [searchTerm, setSearchTerm] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<Party | null>(null)

  const filteredParties =
    parties?.filter(
      (party) =>
        party.firstName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        party.lastName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        party.email.toLowerCase().includes(searchTerm.toLowerCase())
    ) ?? []

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

  if (isLoading) {
    return <LoadingSkeleton rows={5} columns={5} />
  }

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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <TextField
          placeholder="Search parties..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          size="small"
          sx={{ width: 300 }}
        />
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
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredParties.map((party) => (
              <TableRow
                key={party.id}
                hover
                sx={{ cursor: 'pointer' }}
                onClick={() => navigate(`/parties/${party.id}`)}
              >
                <TableCell>
                  {party.firstName} {party.lastName}
                </TableCell>
                <TableCell>{party.email}</TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    {party.roles.map((role) => (
                      <RoleBadge key={role} role={role} />
                    ))}
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation()
                      onEdit?.(party)
                    }}
                  >
                    <EditIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    color="error"
                    onClick={(e) => {
                      e.stopPropagation()
                      setDeleteTarget(party)
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
