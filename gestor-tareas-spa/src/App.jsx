import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import { AppLayout } from './components/AppLayout'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DashboardPage } from './pages/DashboardPage'
import { BoardPage } from './pages/BoardPage'
import { ProfilePage } from './pages/ProfilePage'
import { NotFoundPage } from './pages/NotFoundPage'

function ProtectedRoute() {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <AppLayout /> : <Navigate to="/login" replace />
}

export function App() {
  const { isAuthenticated } = useAuth()
  return <Routes>
    <Route path="/login" element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />} />
    <Route path="/registro" element={isAuthenticated ? <Navigate to="/" replace /> : <RegisterPage />} />
    <Route element={<ProtectedRoute />}>
      <Route index element={<DashboardPage />} />
      <Route path="/proyectos/:projectId" element={<BoardPage />} />
      <Route path="/perfil" element={<ProfilePage />} />
    </Route>
    <Route path="*" element={<NotFoundPage />} />
  </Routes>
}
