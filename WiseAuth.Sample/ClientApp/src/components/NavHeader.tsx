import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { buttonClass, navLinkClass } from '../lib/uiHelpers'

interface NavHeaderProps {
  title: string
}

export function NavHeader({ title }: NavHeaderProps) {
  const { user, logout, hasPermission } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <header className="mb-4 flex items-center justify-between gap-4">
      <div className="flex items-baseline gap-5">
        <h1 className="m-0 text-xl font-semibold">{title}</h1>
        <nav className="flex gap-3">
          <NavLink to="/products" className={({ isActive }) => navLinkClass(isActive)}>
            Products
          </NavLink>
          {(hasPermission('users', 1) || hasPermission('roles', 1)) && (
            <NavLink to="/security/users" className={navLinkClass(location.pathname.startsWith('/security'))}>
              Security
            </NavLink>
          )}
        </nav>
      </div>
      <div className="flex items-center gap-3 text-sm">
        <span>
          Signed in as <strong>{user?.displayName}</strong>
        </span>
        <button className={buttonClass(false)} onClick={handleLogout}>
          Sign out
        </button>
      </div>
    </header>
  )
}
