import axios from 'axios'

const SESSION_KEY = 'taskrino_session'
const SESSION_EVENT = 'taskrino:session'
const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5080/api/v1'

export const apiAssetUrl = (path) => {
  if (!path) return null
  if (/^https?:\/\//i.test(path)) return path
  return new URL(path, baseURL).toString()
}

export const readSession = () => {
  try { return JSON.parse(localStorage.getItem(SESSION_KEY)) }
  catch { return null }
}
export const saveSession = (session) => {
  if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session))
  else localStorage.removeItem(SESSION_KEY)
  window.dispatchEvent(new CustomEvent(SESSION_EVENT, { detail: session }))
}

export { SESSION_EVENT }

export const api = axios.create({ baseURL, timeout: 20000 })

api.interceptors.request.use((config) => {
  const token = readSession()?.accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let refreshPromise = null
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const isAuthRequest = original?.url?.includes('/auth/')
    if (error.response?.status !== 401 || original?._retried || isAuthRequest) throw error
    const session = readSession()
    if (!session?.refreshToken) {
      saveSession(null)
      throw error
    }
    original._retried = true
    try {
      refreshPromise ||= axios.post(`${baseURL}/auth/refresh`, { refreshToken: session.refreshToken }, { timeout: 20000 })
        .then(({ data }) => { saveSession(data); return data })
        .finally(() => { refreshPromise = null })
      const renewed = await refreshPromise
      original.headers.Authorization = `Bearer ${renewed.accessToken}`
      return api(original)
    } catch (refreshError) {
      saveSession(null)
      throw refreshError
    }
  },
)

export function apiError(error, fallback = 'No pudimos completar la acción.') {
  const data = error?.response?.data
  if (data?.errors) return Object.values(data.errors).flat().join(' ')
  return data?.detail || data?.title || (error?.code === 'ECONNABORTED' ? 'La solicitud tardó demasiado. Inténtalo de nuevo.' : fallback)
}
