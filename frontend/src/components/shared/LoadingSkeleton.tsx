import { Skeleton, Box, Stack } from '@mui/material'

interface LoadingSkeletonProps {
  rows?: number
  columns?: number
}

export const LoadingSkeleton: React.FC<LoadingSkeletonProps> = ({ rows = 5, columns = 4 }) => {
  return (
    <Stack spacing={2}>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <Box key={rowIndex} sx={{ display: 'flex', gap: 2 }}>
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} variant="rectangular" height={40} sx={{ flex: 1 }} />
          ))}
        </Box>
      ))}
    </Stack>
  )
}
