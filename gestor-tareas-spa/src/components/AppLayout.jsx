import { LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { Brand } from './Brand'
import { Avatar } from './Avatar'

export function AppLayout() {
  const { user, logout } = useAuth()
  return <div className="app-shell">
    <header className="topbar">
      <Brand />
      <nav className="topnav" aria-label="Navegación principal">
        <NavLink to="/" end>Proyectos</NavLink>
      </nav>
      <div className="account-menu">
        <NavLink className="account-link" to="/perfil"><Avatar name={user?.name} imageUrl={user?.profileImageUrl} /><span className="account-name">{user?.name}</span></NavLink>
        <button className="icon-button" onClick={logout} title="Cerrar sesión" aria-label="Cerrar sesión"><LogOut size={18} /></button>
      </div>
    </header>
    <main className="main-content"><Outlet /></main>
  </div>
}
