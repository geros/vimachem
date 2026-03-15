import { useState } from 'react'
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Collapse,
  TextField,
  Button,
  Tooltip,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
} from '@mui/material'
import {
  KeyboardArrowDown as KeyboardArrowDownIcon,
  KeyboardArrowUp as KeyboardArrowUpIcon,
  FileDownload as ExportIcon,
  Refresh as RefreshIcon,
  FilterAltOff as ClearIcon,
} from '@mui/icons-material'
import { useAllEvents } from '@/hooks/useEvents'
import { Pagination } from '@/components/shared/Pagination'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { formatRelativeTime } from '@/utils/formatters'
import type { EventFilter, LibraryEvent } from '@/types/event'
import { useQueryClient } from '@tanstack/react-query'

const ENTITY_TYPES = ['Party', 'Book', 'Borrowing']
const ACTIONS = ['Created', 'Updated', 'Borrowed', 'Returned', 'Deleted']

const getActionColor = (action: string): 'success' | 'warning' | 'error' | 'info' | 'default' => {
  switch (action.toLowerCase()) {
    case 'created':  return 'success'
    case 'updated':  return 'warning'
    case 'deleted':  return 'error'
    case 'borrowed': return 'info'
    case 'returned': return 'default'
    default:         return 'default'
  }
}

const EventRow: React.FC<{ event: LibraryEvent }> = ({ event }) => {
  const [open, setOpen] = useState(false)

  return (
    <>
      <TableRow hover>
        <TableCell>
          <IconButton size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
          </IconButton>
        </TableCell>
        <TableCell>{formatRelativeTime(event.timestamp)}</TableCell>
        <TableCell>
          <Chip label={event.entityType} size="small" variant="outlined" />
        </TableCell>
        <TableCell>
          <Chip label={event.action} color={getActionColor(event.action)} size="small" />
        </TableCell>
        <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem', color: 'text.secondary' }}>
          {event.eventType}
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={5}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ m: 2 }}>
              <Typography variant="subtitle2" gutterBottom fontWeight={600}>
                Event Details
              </Typography>
              <Box sx={{ display: 'flex', gap: 2, mb: 2, flexWrap: 'wrap' }}>
                <Box sx={{ p: 1.5, bgcolor: '#F5F7FA', borderRadius: 1, minWidth: 200 }}>
                  <Typography variant="caption" color="textSecondary" display="block">Event ID</Typography>
                  <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.8em' }}>{event.id}</Typography>
                </Box>
                <Box sx={{ p: 1.5, bgcolor: '#F5F7FA', borderRadius: 1, minWidth: 200 }}>
                  <Typography variant="caption" color="textSecondary" display="block">Entity ID</Typography>
                  <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.8em' }}>{event.entityId}</Typography>
                </Box>
                <Box sx={{ p: 1.5, bgcolor: '#F5F7FA', borderRadius: 1 }}>
                  <Typography variant="caption" color="textSecondary" display="block">Timestamp</Typography>
                  <Typography variant="body2">{new Date(event.timestamp).toLocaleString()}</Typography>
                </Box>
              </Box>
              {Object.keys(event.relatedEntityIds ?? {}).length > 0 && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mb: 0.5 }}>
                    Related Entities
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    {Object.entries(event.relatedEntityIds).map(([k, v]) => (
                      <Box key={k} sx={{ p: 1, bgcolor: '#F5F7FA', borderRadius: 1 }}>
                        <Typography variant="caption" color="textSecondary" display="block">{k}</Typography>
                        <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.8em' }}>{v}</Typography>
                      </Box>
                    ))}
                  </Box>
                </Box>
              )}
              {event.payload !== undefined && event.payload !== null && (
                <>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mb: 0.5 }}>
                    Payload
                  </Typography>
                  <Box
                    component="pre"
                    sx={{ fontSize: '0.8rem', bgcolor: 'grey.100', p: 2, borderRadius: 1, overflow: 'auto', maxHeight: 200 }}
                  >
                    {JSON.stringify(event.payload, null, 2)}
                  </Box>
                </>
              )}
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  )
}

const EMPTY_FILTER: EventFilter = {}

const EventLog: React.FC = () => {
  const [filter, setFilter] = useState<EventFilter>(EMPTY_FILTER)
  const [pendingFilter, setPendingFilter] = useState<EventFilter>(EMPTY_FILTER)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const queryClient = useQueryClient()
  const { data, isLoading } = useAllEvents(filter, page, pageSize)

  const hasActiveFilters = Object.values(filter).some(Boolean)

  const applyFilters = () => {
    setFilter(pendingFilter)
    setPage(1)
  }

  const clearFilters = () => {
    setPendingFilter(EMPTY_FILTER)
    setFilter(EMPTY_FILTER)
    setPage(1)
  }

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['events'] })
  }

  const handleExport = () => {
    if (!data?.items.length) return
    const json = JSON.stringify(data.items, null, 2)
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `audit-events-${new Date().toISOString().split('T')[0]}.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
            Audit Event Log
          </Typography>
          <Typography variant="body2" color="textSecondary">
            {data ? `${data.totalCount.toLocaleString()} total events` : 'Track all system events'}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Refresh">
            <IconButton onClick={handleRefresh}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
          <Button
            variant="outlined"
            startIcon={<ExportIcon />}
            onClick={handleExport}
            disabled={!data?.items.length}
          >
            Export JSON
          </Button>
        </Box>
      </Box>

      {/* Filter bar */}
      <Paper variant="outlined" sx={{ p: 2, mb: 3 }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'flex-end' }}>
          <FormControl size="small" sx={{ minWidth: 160 }}>
            <InputLabel>Entity Type</InputLabel>
            <Select
              label="Entity Type"
              value={pendingFilter.entityType ?? ''}
              onChange={(e) => setPendingFilter(f => ({ ...f, entityType: e.target.value || undefined }))}
            >
              <MenuItem value=""><em>All</em></MenuItem>
              {ENTITY_TYPES.map(t => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 160 }}>
            <InputLabel>Action</InputLabel>
            <Select
              label="Action"
              value={pendingFilter.action ?? ''}
              onChange={(e) => setPendingFilter(f => ({ ...f, action: e.target.value || undefined }))}
            >
              <MenuItem value=""><em>All</em></MenuItem>
              {ACTIONS.map(a => <MenuItem key={a} value={a}>{a}</MenuItem>)}
            </Select>
          </FormControl>

          <TextField
            size="small"
            label="Entity ID"
            placeholder="UUID..."
            value={pendingFilter.entityId ?? ''}
            onChange={(e) => setPendingFilter(f => ({ ...f, entityId: e.target.value || undefined }))}
            sx={{ minWidth: 220 }}
          />

          <TextField
            size="small"
            label="From"
            type="date"
            value={pendingFilter.from ?? ''}
            onChange={(e) => setPendingFilter(f => ({ ...f, from: e.target.value || undefined }))}
            InputLabelProps={{ shrink: true }}
          />

          <TextField
            size="small"
            label="To"
            type="date"
            value={pendingFilter.to ?? ''}
            onChange={(e) => setPendingFilter(f => ({ ...f, to: e.target.value || undefined }))}
            InputLabelProps={{ shrink: true }}
          />

          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button variant="contained" size="small" onClick={applyFilters}>
              Apply
            </Button>
            {hasActiveFilters && (
              <Button
                variant="outlined"
                size="small"
                startIcon={<ClearIcon />}
                onClick={clearFilters}
              >
                Clear
              </Button>
            )}
          </Box>
        </Box>
      </Paper>

      {/* Table */}
      {isLoading ? (
        <LoadingSkeleton rows={8} columns={5} />
      ) : (
        <TableContainer component={Paper}>
          <Table stickyHeader>
            <TableHead>
              <TableRow sx={{ backgroundColor: '#F5F7FA' }}>
                <TableCell />
                <TableCell>Timestamp</TableCell>
                <TableCell>Entity Type</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Event Type</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((event) => (
                <EventRow key={event.id} event={event} />
              ))}
              {!data?.items.length && (
                <TableRow>
                  <TableCell colSpan={5} align="center">
                    <EmptyState title="No events found" />
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {data && data.totalCount > 0 && (
        <Pagination
          count={data.totalCount}
          page={page}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
        />
      )}
    </Box>
  )
}

export default EventLog
