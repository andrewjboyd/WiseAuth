import { useEffect, useState, type FormEvent } from 'react'
import { NavHeader } from '../components/NavHeader'
import { PermissionEditor, type PermissionsSchema } from '../components/PermissionEditor'
import { SecurityTabs } from '../components/SecurityTabs'
import { ApiError, apiFetch, ValidationError } from '../lib/api'
import { useAuth } from '../lib/AuthContext'
import { bannerClass, buttonClass, inputClass, lockedTitle, pageShellClass, thClass, tdClass } from '../lib/uiHelpers'

interface RoleSummary {
  id: string
  name: string
  permissions: Record<string, number>
}

interface Banner {
  kind: 'locked' | 'error'
  text: string
}

export function RolesPage() {
  const [roles, setRoles] = useState<RoleSummary[]>([])
  const [schema, setSchema] = useState<PermissionsSchema>({})
  const [banner, setBanner] = useState<Banner | null>(null)
  const [createName, setCreateName] = useState('')
  const [createPermissions, setCreatePermissions] = useState<Record<string, number>>({})
  const [createErrors, setCreateErrors] = useState<string[]>([])
  const { hasPermission } = useAuth()

  const canManage = hasPermission('roles', 2)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    try {
      const [schemaResponse, rolesResponse] = await Promise.all([
        apiFetch('/api/auth/permissions-schema'),
        apiFetch('/api/roles'),
      ])
      setSchema(await schemaResponse.json())
      setRoles(await rolesResponse.json())
    } catch (err) {
      reportError(err, 'View')
    }
  }

  function reportError(err: unknown, permission: string) {
    if (err instanceof ApiError && err.status === 403) {
      setBanner({
        kind: 'locked',
        text: `🔒 Blocked — your account doesn't have the ${permission} permission for Roles. The server returned 403.`,
      })
      return
    }
    setBanner({ kind: 'error', text: err instanceof ApiError ? err.message : 'Something went wrong' })
  }

  function updateRolePermissions(roleId: string, claimType: string, bitmask: number) {
    setRoles((prev) =>
      prev.map((r) => (r.id === roleId ? { ...r, permissions: { ...r.permissions, [claimType]: bitmask } } : r)),
    )
  }

  async function handleSave(roleId: string) {
    setBanner(null)
    const role = roles.find((r) => r.id === roleId)
    if (!role) {
      return
    }
    try {
      await apiFetch(`/api/roles/${roleId}/permissions`, {
        method: 'PUT',
        body: JSON.stringify({ permissions: role.permissions }),
      })
      await load()
    } catch (err) {
      reportError(err, 'Manage')
    }
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    setBanner(null)
    setCreateErrors([])
    try {
      await apiFetch('/api/roles', {
        method: 'POST',
        body: JSON.stringify({ name: createName, permissions: createPermissions }),
      })
      setCreateName('')
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

      {roles.length > 0 && (
        <table className="mb-4 w-full border-collapse overflow-hidden rounded-lg bg-white">
          <thead>
            <tr>
              <th className={thClass}>Name</th>
              <th className={thClass}>Permissions</th>
              <th className={thClass} />
            </tr>
          </thead>
          <tbody>
            {roles.map((role) => (
              <tr key={role.id}>
                <td className={tdClass}>{role.name}</td>
                <td className={tdClass}>
                  <PermissionEditor
                    schema={schema}
                    value={role.permissions}
                    onChange={(claimType, bitmask) => updateRolePermissions(role.id, claimType, bitmask)}
                  />
                </td>
                <td className={tdClass}>
                  <button
                    className={buttonClass(!canManage)}
                    title={lockedTitle(canManage, 'Manage')}
                    onClick={() => handleSave(role.id)}
                  >
                    {!canManage && '🔒 '}Save
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <form className="flex flex-wrap items-center gap-2 rounded-lg bg-white p-4" onSubmit={handleCreate}>
        <h2 className="mb-2 w-full text-base font-semibold">{!canManage && '🔒 '}New role</h2>
        {!canManage && (
          <p className="mb-2 w-full text-sm text-gray-600">
            Your account lacks the Manage permission — submitting this will be blocked by the server (403).
          </p>
        )}
        <input
          className={inputClass}
          placeholder="Name"
          value={createName}
          onChange={(e) => setCreateName(e.target.value)}
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
