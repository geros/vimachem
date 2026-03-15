import React from 'react'
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Box,
} from '@mui/material'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  Dashboard as DashboardIcon,
  People as PeopleIcon,
  MenuBook as BookIcon,
  SwapHoriz as BorrowingIcon,
  Category as CategoryIcon,
  History as HistoryIcon,
} from '@mui/icons-material'

const drawerWidth = 240

const navigationItems = [
  { path: '/', label: 'Dashboard', icon: DashboardIcon },
  { path: '/parties', label: 'Parties', icon: PeopleIcon },
  { path: '/books', label: 'Books', icon: BookIcon },
  { path: '/borrowings', label: 'Borrowings', icon: BorrowingIcon },
  { path: '/categories', label: 'Categories', icon: CategoryIcon },
  { path: '/audit', label: 'Audit Log', icon: HistoryIcon },
]

interface SidebarProps {
  mobileOpen?: boolean
  onClose?: () => void
}

export const Sidebar: React.FC<SidebarProps> = ({ mobileOpen = false, onClose }) => {
  const navigate = useNavigate()
  const location = useLocation()

  const drawer = (
    <Box>
      <Toolbar />
      <List>
        {navigationItems.map((item) => {
          const Icon = item.icon
          const isActive =
            location.pathname === item.path ||
            (item.path !== '/' && location.pathname.startsWith(`${item.path}`))
          return (
            <ListItem key={item.path} disablePadding>
              <ListItemButton
                selected={isActive}
                onClick={() => {
                  navigate(item.path)
                  onClose?.()
                }}
                sx={{
                  '&.Mui-selected': {
                    backgroundColor: '#D5E8F0',
                    borderRight: '3px solid #0077C8',
                  },
                }}
              >
                <ListItemIcon>
                  <Icon sx={{ color: isActive ? '#0077C8' : 'inherit' }} />
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  sx={{ color: isActive ? '#0077C8' : 'inherit' }}
                />
              </ListItemButton>
            </ListItem>
          )
        })}
      </List>
    </Box>
  )

  return (
    <>
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onClose}
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: 'block', sm: 'none' },
          '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
        }}
      >
        {drawer}
      </Drawer>
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', sm: 'block' },
          '& .MuiDrawer-paper': {
            boxSizing: 'border-box',
            width: drawerWidth,
            backgroundColor: '#F5F7FA',
            borderRight: '1px solid #E0E4E8',
          },
        }}
        open
      >
        {drawer}
      </Drawer>
    </>
  )
}
