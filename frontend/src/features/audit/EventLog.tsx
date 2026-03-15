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
  Tabs,
  Tab,
  TextField,
  Autocomplete,
  Button,
  Tooltip,
} from '@mui/material'
import {
  KeyboardArrowDown as KeyboardArrowDownIcon,
  KeyboardArrowUp as KeyboardArrowUpIcon,
  FileDownload as ExportIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material'
import { usePartyEvents, useBookEvents } from '@/hooks/useEvents'
import { useParties } from '@/hooks/useParties'
import { useBooks } from '@/hooks/useBooks'
import { Pagination } from '@/components/shared/Pagination'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'
import { EmptyState } from '@/components/shared/EmptyState'
import { formatRelativeTime } from '@/utils/formatters'
import type { LibraryEvent } from '@/types/event'
import { useQueryClient } from '@tanstack/react-query'

interface EventRowProps {
  event: LibraryEvent
}

const getActionColor = (action: string): 'success' | 'warning' | 'error' | 'info' | 'default' => {
  switch (action.toLowerCase()) {
    case 'created': return 'success'
    case 'updated': return 'warning'
    case 'deleted': return 'error'
    case 'borrowed': return 'info'
    case 'returned': return 'default'
    default: return 'default'
  }
}

const EventRow: React.FC<EventRowProps> = ({ event }) => {
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
        <TableCell>{event.eventType}</TableCell>
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
              {event.payload && (
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

const EventLog: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'party' | 'book'>('party')
  const [selectedParty, setSelectedParty] = useState<{ id: string; name: string } | null>(null)
  const [selectedBook, setSelectedBook] = useState<{ id: string; title: string } | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const queryClient = useQueryClient()
  const { data: parties } = useParties()
  const { data: books } = useBooks()

  const { data: partyEvents, isLoading: partyEventsLoading } = usePartyEvents(selectedParty?.id ?? '', page, pageSize)
  const { data: bookEvents, isLoading: bookEventsLoading } = useBookEvents(selectedBook?.id ?? '', page, pageSize)

  const currentData = activeTab === 'party' ? partyEvents : bookEvents
  const isLoading = activeTab === 'party' ? partyEventsLoading : bookEventsLoading

  const handleTabChange = (_: React.SyntheticEvent, newValue: 'party' | 'book') => {
    setActiveTab(newValue)
    setPage(1)
    setSelectedParty(null)
    setSelectedBook(null)
  }

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['events'] })
  }

  const handleExport = () => {
    if (!currentData?.items.length) return
    const json = JSON.stringify(currentData.items, null, 2)
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `audit-events-${activeTab}-${new Date().toISOString().split('T')[0]}.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Box>
          <Typography variant="h4" sx={{ color: '#003B6F', fontWeight: 600 }}>
            Audit Event Log
          </Typography>
          <Typography variant="body2" color="textSecondary">
            Track all system events across parties and books
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
            disabled={!currentData?.items.length}
          >
            Export JSON
          </Button>
        </Box>
      </Box>

      <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
        <Tab label="Party Events" value="party" />
        <Tab label="Book Events" value="book" />
      </Tabs>

      <Box sx={{ mb: 3 }}>
        {activeTab === 'party' ? (
          <Autocomplete
            options={parties?.map((p) => ({ id: p.id, name: `${p.firstName} ${p.lastName}` })) ?? []}
            getOptionLabel={(option) => option.name}
            value={selectedParty}
            onChange={(_, newValue) => { setSelectedParty(newValue); setPage(1) }}
            renderInput={(params) => <TextField {...params} label="Select Party" fullWidth />}
            isOptionEqualToValue={(option, value) => option.id === value.id}
          />
        ) : (
          <Autocomplete
            options={books?.map((b) => ({ id: b.id, title: b.title })) ?? []}
            getOptionLabel={(option) => option.title}
            value={selectedBook}
            onChange={(_, newValue) => { setSelectedBook(newValue); setPage(1) }}
            renderInput={(params) => <TextField {...params} label="Select Book" fullWidth />}
            isOptionEqualToValue={(option, value) => option.id === value.id}
          />
        )}
      </Box>

      {!selectedParty && activeTab === 'party' && (
        <EmptyState title="Select a party to view events" />
      )}
      {!selectedBook && activeTab === 'book' && (
        <EmptyState title="Select a book to view events" />
      )}

      {(selectedParty || selectedBook) && (
        <>
          {isLoading ? (
            <LoadingSkeleton rows={5} columns={5} />
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
                  {currentData?.items.map((event) => (
                    <EventRow key={event.id} event={event} />
                  ))}
                  {!currentData?.items.length && (
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

          {currentData && currentData.totalCount > 0 && (
            <Pagination
              count={currentData.totalCount}
              page={page}
              pageSize={pageSize}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </>
      )}
    </Box>
  )
}

export default EventLog
