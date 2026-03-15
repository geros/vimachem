import { TablePagination } from '@mui/material'

interface PaginationProps {
  count: number
  page: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange: (pageSize: number) => void
  pageSizeOptions?: number[]
}

export const Pagination: React.FC<PaginationProps> = ({
  count,
  page,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
}) => {
  return (
    <TablePagination
      component="div"
      count={count}
      page={page - 1}
      onPageChange={(_, newPage) => onPageChange(newPage + 1)}
      rowsPerPage={pageSize}
      onRowsPerPageChange={(e) => onPageSizeChange(parseInt(e.target.value, 10))}
      rowsPerPageOptions={pageSizeOptions}
      labelDisplayedRows={({ from, to, count }) => `${from}-${to} of ${count}`}
    />
  )
}
