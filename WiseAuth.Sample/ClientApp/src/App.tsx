import type { ReactNode } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './lib/AuthContext'
import { AccessControlsPage } from './pages/AccessControlsPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { ProductsPage } from './pages/ProductsPage'
import { RolesPage } from './pages/RolesPage'
import { SecurityUsersPage } from './pages/SecurityUsersPage'
import { UserDetailPage } from './pages/UserDetailPage'

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  if (loading) {
    return <p className="p-8 text-center">Loading…</p>
  }
  if (!user) {
    return <Navigate to="/login" replace />
  }
  return <>{children}</>
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/products"
        element={
          <RequireAuth>
            <ProductsPage />
          </RequireAuth>
        }
      />
      <Route
        path="/security/users"
        element={
          <RequireAuth>
            <SecurityUsersPage />
          </RequireAuth>
        }
      />
      <Route
        path="/security/users/:id/:tab"
        element={
          <RequireAuth>
            <UserDetailPage />
          </RequireAuth>
        }
      />
      <Route
        path="/security/roles"
        element={
          <RequireAuth>
            <RolesPage />
          </RequireAuth>
        }
      />
      <Route
        path="/security/access-controls"
        element={
          <RequireAuth>
            <AccessControlsPage />
          </RequireAuth>
        }
      />
      <Route path="/users" element={<Navigate to="/security/users" replace />} />
      <Route path="/" element={<Navigate to="/products" replace />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  )
}
