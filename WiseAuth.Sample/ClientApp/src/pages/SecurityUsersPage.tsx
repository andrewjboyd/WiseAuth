import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { NavHeader } from '../components/NavHeader'
import { PermissionEditor, type PermissionsSchema } from '../components/PermissionEditor'
import { PermissionSummaryList } from '../components/PermissionSummaryList'
import { SecurityTabs } from '../components/SecurityTabs'
import { ApiError, apiFetch, ValidationError } from '../lib/api'
import { useAuth } from '../lib/AuthContext'
import { bannerClass, buttonClass, inputClass, lockedTitle, pageShellClass, thClass, tdClass } from '../lib/uiHelpers'

interface UserSummary {
  id: string
  userName: string
  displayName: string
  email: string
  effectivePermissions: Record<string, number>
}

interface Banner {
  kind: 'locked' | 'error'
  text: string
}

const emptyCreateForm = { userName: '', displayName: '', email: '', password: '' }

export function SecurityUsersPage() {
  const [users, setUsers] = useState<UserSummary[]>([])
  const [schema, setSchema] = useState<PermissionsSchema>({})
  const [banner, setBanner] = useState<Banner | null>(null)
  const [createForm, setCreateForm] = useState(emptyCreateForm)
  const [createPermissions, setCreatePermissions] = useState<Record<string, number>>({})
  const [createErrors, setCreateErrors] = useState<string[]>([])
  const { hasPermission } = useAuth()

  const canManage = hasPermission('users', 2)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    try {
      const [schemaResponse, usersResponse] = await Promise.all([
        apiFetch('/api/auth/permissions-schema'),
        apiFetch('/api/users'),
      ])
      setSchema(await schemaResponse.json())
      setUsers(await usersResponse.json())
    } catch (err) {
      reportError(err, 'View')
    }
  }

  function reportError(err: unknown, permission: string) {
    if (err instanceof ApiError && err.status === 403) {
      setBanner({
        kind: 'locked',
        text: `🔒 Blocked — your account doesn't have the ${permission} permission for Users. The server returned 403.`,
      })
      return
    }
    setBanner({ kind: 'error', text: err instanceof ApiError ? err.message : 'Something went wrong' })
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    setBanner(null)
    setCreateErrors([])
    try {
      await apiFetch('/api/users', {
        method: 'POST',
        body: JSON.stringify({ ...createForm, permissions: createPermissions }),
      })
      setCreateForm(emptyCreateForm)
      setCreatePermissions({})
      await load()
    } catch (err) {
      if (err instanceof ValidationError) {
        setCreateErrors(err.errors)
        return
      }
      reportError(err, 'Manage')
    }
  }

  return (
    <div className={pageShellClass}>
      <NavHeader title="Security" />
      <SecurityTabs />

      {banner && <p className={bannerClass(banner.kind)}>{banner.text}</p>}

      {users.length > 0 && (
        <table className="mb-4 w-full border-collapse overflow-hidden rounded-lg bg-white">
          <thead>
            <tr>
              <th className={thClass}>Username</th>
              <th className={thClass}>Display name</th>
              <th className={thClass}>Email</th>
              <th className={thClass}>Effective permissions</th>
              <th className={thClass} />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td className={tdClass}>{user.userName}</td>
                <td className={tdClass}>{user.displayName}</td>
                <td className={tdClass}>{user.email}</td>
                <td className={tdClass}>
                  <PermissionSummaryList schema={schema} value={user.effectivePermissions} />
                </td>
                <td className={tdClass}>
                  <Link className="text-sm text-blue-600 hover:underline" to={`/security/users/${user.id}/profile`}>
                    Edit
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <form className="flex flex-wrap items-center gap-2 rounded-lg bg-white p-4" onSubmit={handleCreate}>
        <h2 className="mb-2 w-full text-base font-semibold">{!canManage && '🔒 '}New user</h2>
        {!canManage && (
          <p className="mb-2 w-full text-sm text-gray-600">
            Your account lacks the Manage permission — submitting this will be blocked by the server (403).
          </p>
        )}
        <input
          className={inputClass}
          placeholder="Username"
          value={createForm.userName}
          onChange={(e) => setCreateForm({ ...createForm, userName: e.target.value })}
          required
        />
        <input
          className={inputClass}
          placeholder="Display name"
          value={createForm.displayName}
          onChange={(e) => setCreateForm({ ...createForm, displayName: e.target.value })}
          required
        />
        <input
          className={inputClass}
          placeholder="Email"
          type="email"
          value={createForm.email}
          onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
          required
        />
        <input
          className={inputClass}
          placeholder="Password"
          type="password"
          value={createForm.password}
          onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
          required
        />
        <PermissionEditor
          schema={schema}
          value={createPermissions}
          onChange={(claimType, bitmask) => setCreatePermissions({ ...createPermissions, [claimType]: bitmask })}
        />
        {createErrors.length > 0 && (
          <ul className="m-0 w-full list-disc pl-5 text-sm text-red-700">
            {createErrors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        )}
        <button type="submit" className={buttonClass(!canManage)} title={lockedTitle(canManage, 'Manage')}>
          {!canManage && '🔒 '}Create
        </button>
      </form>
    </div>
  )
}
