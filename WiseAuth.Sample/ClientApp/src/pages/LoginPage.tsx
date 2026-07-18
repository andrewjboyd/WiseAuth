import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { buttonClass, inputClass } from '../lib/uiHelpers'

export function LoginPage() {
  const [userName, setUserName] = useState('Admin')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await login(userName, password)
      navigate('/products')
    } catch {
      setError('Invalid username or password')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-100">
      <form
        className="flex w-80 flex-col gap-3 rounded-xl bg-white p-8 shadow-sm"
        onSubmit={handleSubmit}
      >
        <h1 className="mb-1 text-xl font-semibold">WiseAuth Sample</h1>
        <p className="mb-2 text-sm text-gray-600">
          Try <code>Admin</code> / <code>Admin123!</code> (full access) or <code>Viewer</code> /{' '}
          <code>Viewer123!</code> (read-only)
        </p>
        <label className="flex flex-col gap-1 text-sm text-gray-700">
          Username
          <input className={inputClass} value={userName} onChange={(e) => setUserName(e.target.value)} autoFocus />
        </label>
        <label className="flex flex-col gap-1 text-sm text-gray-700">
          Password
          <input
            className={inputClass}
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        {error && <p className="text-sm text-red-700">{error}</p>}
        <button type="submit" className={buttonClass(false)} disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
