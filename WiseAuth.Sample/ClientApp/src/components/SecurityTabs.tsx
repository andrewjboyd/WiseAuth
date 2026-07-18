import { NavLink, useLocation } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { navLinkClass } from '../lib/uiHelpers'

// Sub-nav for the Security area (Users | Roles | Access Controls). The Users tab
// also covers nested user-detail routes (/security/users/:id/:tab), so its active
// state can't rely on NavLink's own exact-match default.
export function SecurityTabs() {
  const { hasPermission } = useAuth()
  const location = useLocation()

  return (
    <nav className="mb-4 flex gap-4 border-b border-gray-200 pb-2">
      {hasPermission('users', 1) && (
        <NavLink to="/security/users" className={navLinkClass(location.pathname.startsWith('/security/users'))}>
          Users
        </NavLink>
      )}
      {hasPermission('roles', 1) && (
        <NavLink to="/security/roles" className={({ isActive }) => navLinkClass(isActive)}>
          Roles
        </NavLink>
      )}
      <NavLink to="/security/access-controls" className={({ isActive }) => navLinkClass(isActive)}>
        Access Controls
      </NavLink>
    </nav>
  )
}
