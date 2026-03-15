import { createTheme } from '@mui/material/styles'

export const colors = {
  primaryBlue: '#0077C8',
  primaryDark: '#003B6F',
  primaryLight: '#D5E8F0',
  teal: '#00A3B5',
  lightTeal: '#E0F4F4',
  success: '#28A745',
  danger: '#DC3545',
  warning: '#F5A623',
  background: '#F5F7FA',
  surface: '#FFFFFF',
  border: '#E0E4E8',
  textPrimary: '#2D3748',
  textSecondary: '#6B7280',
}

export const theme = createTheme({
  palette: {
    primary: {
      main: colors.primaryBlue,
      dark: colors.primaryDark,
      light: colors.primaryLight,
    },
    secondary: {
      main: colors.teal,
      light: colors.lightTeal,
    },
    success: {
      main: colors.success,
    },
    error: {
      main: colors.danger,
    },
    warning: {
      main: colors.warning,
    },
    background: {
      default: colors.background,
      paper: colors.surface,
    },
    text: {
      primary: colors.textPrimary,
      secondary: colors.textSecondary,
    },
    divider: colors.border,
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: { fontWeight: 600 },
    h2: { fontWeight: 600 },
    h3: { fontWeight: 600 },
    h4: { fontWeight: 600 },
    h5: { fontWeight: 600 },
    h6: { fontWeight: 600 },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 8,
          fontWeight: 500,
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          height: 28,
          fontWeight: 500,
        },
      },
    },
  },
})
