import { useEffect, useState, type FormEvent } from 'react'
import { Link, NavLink, useParams } from 'react-router-dom'
import { NavHeader } from '../components/NavHeader'
import { PermissionEditor, type PermissionsSchema } from '../components/PermissionEditor'
import { SecurityTabs } from '../components/SecurityTabs'
import { ApiError, apiFetch, ValidationError } from '../lib/api'
import { useAuth } from '../lib/AuthContext'
import { bannerClass, buttonClass, classNames, inputClass, lockedTitle, navLinkClass, pageShellClass } from '../lib/uiHelpers'

interface UserDetail {
  id: string
  userName: string
  displayName: string
  email: string
}

interface Banner {
  kind: 'locked' | 'error'
  text: string
}

const emptyProfileForm = { userName: '', displayName: '', email: '' }

export function UserDetailPage() {
  const { id, tab } = useParams<{ id: string; tab: string }>()
  const [detail, setDetail] = useState<UserDetail | null>(null)
  const [schema, setSchema] = useState<PermissionsSchema>({})
  const [memberRoleNames, setMemberRoleNames] = useState<string[]>([])
  const [allRoleNames, setAllRoleNames] = useState<string[]>([])
  const [claims, setClaims] = useState<Record<string, number>>({})
  const [profileForm, setProfileForm] = useState(emptyProfileForm)
  const [profileErrors, setProfileErrors] = useState<string[]>([])
  const [banner, setBanner] = useState<Banner | null>(null)
  const { user: currentUser, hasPermission } = useAuth()

  const canManage = hasPermission('users', 2)
  const isSelf = id === currentUser?.id
  const saveAllowed = canManage && !isSelf
  const saveTitle = isSelf
    ? 'You cannot edit your own permissions - this will be blocked by the server (400)'
    : !canManage
      ? "Your account lacks the Manage permission - this will be blocked by the server (403)"
      : undefined

  useEffect(() => {
    void load()
  }, [id, tab])

  async function load() {
    if (!id) {
      return
    }
    setBanner(null)
    setProfileErrors([])
    try {
      const requests = [apiFetch(`/api/users/${id}`), apiFetch('/api/auth/permissions-schema')]
      if (tab === 'claims') {
        requests.push(apiFetch(`/api/users/${id}/claims`))
      } else if (tab === 'roles') {
        requests.push(apiFetch(`/api/users/${id}/roles`))
      }
      const [detailResponse, schemaResponse, tabResponse] = await Promise.all(requests)
      const detailJson: UserDetail = await detailResponse.json()
      setDetail(detailJson)
      setProfileForm({ userName: detailJson.userName, displayName: detailJson.displayName, email: detailJson.email })
      setSchema(await schemaResponse.json())
      if (tab === 'claims' && tabResponse) {
        setClaims(await tabResponse.json())
      } else if (tabResponse) {
        const roles: { memberRoleNames: string[]; allRoleNames: string[] } = await tabResponse.json()
        setMemberRoleNames(roles.memberRoleNames)
        setAllRoleNames(roles.allRoleNames)
      }
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

  function toggleRole(roleName: string, checked: boolean) {
    setMemberRoleNames((prev) => (checked ? [...prev, roleName] : prev.filter((r) => r !== roleName)))
  }

  async function handleSaveRoles() {
    if (!id) {
      return
    }
    setBanner(null)
    try {
      await apiFetch(`/api/users/${id}/roles`, {
        method: 'PUT',
        body: JSON.stringify({ roleNames: memberRoleNames }),
      })
      await load()
    } catch (err) {
      reportError(err, 'Manage')
    }
  }

  async function handleSaveClaims() {
    if (!id) {
      return
    }
    setBanner(null)
    try {
      await apiFetch(`/api/users/${id}/claims`, {
        method: 'PUT',
        body: JSON.stringify({ permissions: claims }),
      })
      await load()
    } catch (err) {
      reportError(err, 'Manage')
    }
  }

  async function handleSaveProfile(event: FormEvent) {
    event.preventDefault()
    if (!id) {
      return
    }
    setBanner(null)
    setProfileErrors([])
    try {
      await apiFetch(`/api/users/${id}/profile`, {
        method: 'PUT',
        body: JSON.stringify(profileForm),
      })
      await load()
    } catch (err) {
      if (err instanceof ValidationError) {
        setProfileErrors(err.errors)
        return
      }
      reportError(err, 'Manage')
    }
  }

  return (
    <div className={pageShellClass}>
      <NavHeader title="Security" />
      <SecurityTabs />
      <Link className="mb-3 inline-block text-sm text-gray-600 hover:underline" to="/security/users">
        ← Back to Users
      </Link>

      {banner && <p className={bannerClass(banner.kind)}>{banner.text}</p>}

      {detail && (
        <>
          <h2 className="mb-3 text-lg font-semibold">
            {detail.displayName} <small className="font-normal text-gray-500">({detail.userName})</small>
          </h2>

          <nav className="mb-4 flex gap-4 border-b border-gray-200 pb-2">
            <NavLink to={`/security/users/${id}/profile`} className={({ isActive }) => navLinkClass(isActive)}>
              Profile
            </NavLink>
            <NavLink to={`/security/users/${id}/roles`} className={({ isActive }) => navLinkClass(isActive)}>
              Roles
            </NavLink>
            <NavLink to={`/security/users/${id}/claims`} className={({ isActive }) => navLinkClass(isActive)}>
              Claims
            </NavLink>
          </nav>

          {tab === 'profile' ? (
            <form className="flex flex-wrap items-start gap-2 rounded-lg bg-white p-4" onSubmit={handleSaveProfile}>
              <label className="min-w-[140px] flex-1 text-sm text-gray-700">
                Username
                <input
                  className={classNames(inputClass, 'mt-1 block w-full')}
                  value={profileForm.userName}
                  onChange={(e) => setProfileForm({ ...profileForm, userName: e.target.value })}
                  required
                />
              </label>
              <label className="min-w-[140px] flex-1 text-sm text-gray-700">
                Display name
                <input
                  className={classNames(inputClass, 'mt-1 block w-full')}
                  value={profileForm.displayName}
                  onChange={(e) => setProfileForm({ ...profileForm, displayName: e.target.value })}
                  required
                />
              </label>
              <label className="min-w-[140px] flex-1 text-sm text-gray-700">
                Email
                <input
                  className={classNames(inputClass, 'mt-1 block w-full')}
                  type="email"
                  value={profileForm.email}
                  onChange={(e) => setProfileForm({ ...profileForm, email: e.target.value })}
                  required
                />
              </label>
              {profileErrors.length > 0 && (
                <ul className="m-0 w-full list-disc pl-5 text-sm text-red-700">
                  {profileErrors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              )}
              <button type="submit" className={buttonClass(!canManage)} title={lockedTitle(canManage, 'Manage')}>
                {!canManage && '🔒 '}Save
              </button>
            </form>
          ) : tab === 'claims' ? (
            <div className="flex flex-col items-start gap-3">
              <PermissionEditor
                schema={schema}
                value={claims}
                onChange={(claimType, bitmask) => setClaims({ ...claims, [claimType]: bitmask })}
              />
              <button className={buttonClass(!saveAllowed)} title={saveTitle} onClick={handleSaveClaims}>
                {!saveAllowed && '🔒 '}Save
              </button>
            </div>
          ) : (
            <div className="flex flex-col items-start gap-3">
              <fieldset className="rounded-md border border-gray-200 px-2.5 py-1.5">
                <legend className="px-1 text-xs uppercase text-gray-500">roles</legend>
                {allRoleNames.map((roleName) => (
                  <label key={roleName} className="flex items-center gap-1.5 whitespace-nowrap text-sm text-gray-700">
                    <input
                      type="checkbox"
                      className="h-3.5 w-3.5"
                      checked={memberRoleNames.includes(roleName)}
                      onChange={(e) => toggleRole(roleName, e.target.checked)}
                    />
                    {roleName}
                  </label>
                ))}
              </fieldset>
              <button className={buttonClass(!saveAllowed)} title={saveTitle} onClick={handleSaveRoles}>
                {!saveAllowed && '🔒 '}Save
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
