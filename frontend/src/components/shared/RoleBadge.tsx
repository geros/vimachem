import { Chip } from '@mui/material'
import { Person as PersonIcon, MenuBook as BookIcon } from '@mui/icons-material'

interface RoleBadgeProps {
  role: string
}

export const RoleBadge: React.FC<RoleBadgeProps> = ({ role }) => {
  const isAuthor = role.toLowerCase() === 'author'

  return (
    <Chip
      icon={isAuthor ? <BookIcon fontSize="small" /> : <PersonIcon fontSize="small" />}
      label={role}
      size="small"
      sx={{
        backgroundColor: isAuthor ? '#0077C8' : '#00A3B5',
        color: 'white',
        '& .MuiChip-icon': {
          color: 'white',
        },
      }}
    />
  )
}
