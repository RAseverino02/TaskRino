import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { api, readSession, saveSession, SESSION_EVENT } from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [session, setSession] = useState(readSession)

  useEffect(() => {
    const sync = (event) => setSession(event.detail)
    window.addEventListener(SESSION_EVENT, sync)
    return () => window.removeEventListener(SESSION_EVENT, sync)
  }, [])

  const login = useCallback(async (credentials) => {
    const { data } = await api.post('/auth/login', credentials)
    saveSession(data)
    return data
  }, [])
  const register = useCallback(async (payload) => {
    const { data } = await api.post('/auth/register', payload)
    saveSession(data)
    return data
  }, [])
  const logout = useCallback(async () => {
    const current = readSession()
    saveSession(null)
    if (current?.refreshToken) {
      try { await api.post('/auth/logout', { refreshToken: current.refreshToken }) } catch { /* sesión local cerrada */ }
    }
  }, [])
  const updateUser = useCallback((user) => {
    const current = readSession()
    if (current) saveSession({ ...current, user })
  }, [])

  const value = useMemo(() => ({ session, user: session?.user || null, isAuthenticated: !!session?.accessToken, login, register, logout, updateUser }), [session, login, register, logout, updateUser])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = () => useContext(AuthContext)
