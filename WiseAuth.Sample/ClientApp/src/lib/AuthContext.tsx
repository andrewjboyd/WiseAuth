import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

interface AuthState {
  id: string
  displayName: string
  permissions: Record<string, number>
}

interface AuthContextValue {
  user: AuthState | null
  loading: boolean
  login: (userName: string, password: string) => Promise<void>
  logout: () => Promise<void>
  hasPermission: (claimType: string, bit: number) => boolean
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    void refresh()
  }, [])

  async function refresh() {
    try {
      const response = await fetch('/api/auth/me', { credentials: 'same-origin' })
      setUser(response.ok ? await response.json() : null)
    } finally {
      setLoading(false)
    }
  }

  async function login(userName: string, password: string) {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userName, password }),
    })

    if (!response.ok) {
      throw new Error('Invalid username or password')
    }

    setUser(await response.json())
  }

  async function logout() {
    await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' })
    setUser(null)
  }

  // Purely a UX convenience for hiding controls the signed-in user can't use -
  // the server re-checks every request, so this can never be the real gate.
  function hasPermission(claimType: string, bit: number) {
    const value = user?.permissions[claimType] ?? 0
    return (value & bit) !== 0
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return ctx
}
