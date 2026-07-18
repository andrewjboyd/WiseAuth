import { useEffect, useState } from 'react'
import { NavHeader } from '../components/NavHeader'
import { PermissionSchemaTable } from '../components/PermissionSchemaTable'
import type { PermissionsSchema } from '../components/PermissionEditor'
import { SecurityTabs } from '../components/SecurityTabs'
import { ApiError, apiFetch } from '../lib/api'
import { bannerClass, hintClass, pageShellClass } from '../lib/uiHelpers'

export function AccessControlsPage() {
  const [schema, setSchema] = useState<PermissionsSchema>({})
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void load()
  }, [])

  async function load() {
    try {
      const response = await apiFetch('/api/auth/permissions-schema')
      setSchema(await response.json())
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong')
    }
  }

  return (
    <div className={pageShellClass}>
      <NavHeader title="Security" />
      <SecurityTabs />
      {error && <p className={bannerClass('error')}>{error}</p>}
      <p className={hintClass}>
        Every permission bit registered with <code>AddWiseAuth&lt;T&gt;()</code>, grouped by claim type. Read-only.
      </p>
      <PermissionSchemaTable schema={schema} />
    </div>
  )
}
