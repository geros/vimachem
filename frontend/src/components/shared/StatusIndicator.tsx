import { Chip } from '@mui/material'
import { CheckCircle, Cancel, Schedule } from '@mui/icons-material'

interface StatusIndicatorProps {
  status: 'available' | 'unavailable' | 'active' | 'returned' | 'borrowed'
  showLabel?: boolean
}

export const StatusIndicator: React.FC<StatusIndicatorProps> = ({
  status,
  showLabel = true,
}) => {
  const config = {
    available: {
      color: '#28A745',
      icon: <CheckCircle fontSize="small" />,
      label: 'Available',
    },
    unavailable: {
      color: '#DC3545',
      icon: <Cancel fontSize="small" />,
      label: 'Unavailable',
    },
    active: {
      color: '#DC3545',
      icon: <Schedule fontSize="small" />,
      label: 'Active',
    },
    returned: {
      color: '#00A3B5',
      icon: <CheckCircle fontSize="small" />,
      label: 'Returned',
    },
    borrowed: {
      color: '#F5A623',
      icon: <Schedule fontSize="small" />,
      label: 'Borrowed',
    },
  }

  const { color, icon, label } = config[status]

  if (!showLabel) {
    return (
      <Box
        sx={{
          width: 12,
          height: 12,
          borderRadius: '50%',
          backgroundColor: color,
        }}
      />
    )
  }

  return (
    <Chip
      icon={icon}
      label={label}
      size="small"
      sx={{
        backgroundColor: `${color}20`,
        color: color,
        '& .MuiChip-icon': {
          color: color,
        },
      }}
    />
  )
}

import { Box } from '@mui/material'
export { Box }
