import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Grid,
  Typography,
  TextField,
  Button,
  Paper,
  FormControlLabel,
  Checkbox,
  Divider,
} from '@mui/material'
import { ArrowBack as ArrowBackIcon } from '@mui/icons-material'
import { useParty, useCreateParty, useUpdateParty } from '@/hooks/useParties'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { useToast } from '@/context/ToastContext'
import { RoleType } from '@/types/party'

const PartyForm: React.FC = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const isEditing = !!id

  const { data: party, isLoading: partyLoading } = useParty(id || '')
  const createMutation = useCreateParty()
  const updateMutation = useUpdateParty()

  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
  })
  const [roles, setRoles] = useState<RoleType[]>([])
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (party) {
      setFormData({
        firstName: party.firstName,
        lastName: party.lastName,
        email: party.email,
      })
      setRoles(
        party.roles.map((r) => (r === 'Author' ? RoleType.Author : RoleType.Customer))
      )
    }
  }, [party])

  const validate = () => {
    const newErrors: Record<string, string> = {}
    if (!formData.firstName.trim()) newErrors.firstName = 'First name is required'
    if (!formData.lastName.trim()) newErrors.lastName = 'Last name is required'
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required'
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Invalid email format'
    }
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({ id: id!, data: formData })
        showSuccess('Party updated successfully')
      } else {
        await createMutation.mutateAsync(formData)
        showSuccess('Party created successfully')
      }
      navigate('/parties')
    } catch {
      showError(isEditing ? 'Failed to update party' : 'Failed to create party')
    }
  }

  if (isEditing && partyLoading) {
    return <LoadingSkeleton rows={3} columns={1} />
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/parties')}>
          Back
        </Button>
        <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
          {isEditing ? 'Edit Party' : 'Add New Party'}
        </Typography>
      </Box>

      <Paper sx={{ p: 4, maxWidth: 600 }}>
        <form onSubmit={handleSubmit}>
          <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
            {isEditing
              ? 'Update the party record details below.'
              : 'Create a new entity record. A party can be an author, customer, or both.'}
          </Typography>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                label="First Name"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                error={!!errors.firstName}
                helperText={errors.firstName}
                fullWidth
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <TextField
                label="Last Name"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                error={!!errors.lastName}
                helperText={errors.lastName}
                fullWidth
              />
            </Grid>
          </Grid>
          <TextField
            label="Email"
            type="email"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            error={!!errors.email}
            helperText={errors.email}
            fullWidth
            sx={{ mb: 3 }}
          />

          <Divider sx={{ my: 3 }} />

          <Typography variant="h6" gutterBottom>
            Roles
          </Typography>
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={roles.includes(RoleType.Author)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setRoles([...roles, RoleType.Author])
                    } else {
                      setRoles(roles.filter((r) => r !== RoleType.Author))
                    }
                  }}
                />
              }
              label="Author"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={roles.includes(RoleType.Customer)}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setRoles([...roles, RoleType.Customer])
                    } else {
                      setRoles(roles.filter((r) => r !== RoleType.Customer))
                    }
                  }}
                />
              }
              label="Customer"
            />
          </Box>
          <Typography variant="caption" color="textSecondary">
            A party can have both Author and Customer roles simultaneously.
          </Typography>

          <Box sx={{ display: 'flex', gap: 2, mt: 4, justifyContent: 'flex-end' }}>
            <Button variant="outlined" onClick={() => navigate('/parties')}>
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

export default PartyForm
