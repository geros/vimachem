import { lazy, Suspense } from 'react'
import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { LoadingSkeleton } from '@/components/shared/LoadingSkeleton'

const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'))
const PartiesPage = lazy(() => import('@/features/parties/PartiesPage'))
const BooksPage = lazy(() => import('@/features/books/BooksPage'))
const BorrowingsPage = lazy(() => import('@/features/borrowings/BorrowingsPage'))
const CategoriesPage = lazy(() => import('@/features/categories/CategoriesPage'))
const AuditPage = lazy(() => import('@/features/audit/AuditPage'))

const withSuspense = (Component: React.ComponentType) => (
  <Suspense fallback={<LoadingSkeleton />}>
    <Component />
  </Suspense>
)

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: withSuspense(DashboardPage) },
      { path: 'parties/*', element: withSuspense(PartiesPage) },
      { path: 'books/*', element: withSuspense(BooksPage) },
      { path: 'borrowings', element: withSuspense(BorrowingsPage) },
      { path: 'categories', element: withSuspense(CategoriesPage) },
      { path: 'audit', element: withSuspense(AuditPage) },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
])
