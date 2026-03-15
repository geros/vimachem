import { Chip } from '@mui/material'
import { CheckCircle, Cancel } from '@mui/icons-material'

interface StatusIndicatorProps {
  status: 'available' | 'unavailable'
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
