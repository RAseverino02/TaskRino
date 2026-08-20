import { useCallback, useState } from 'react'
import { apiError } from '../services/api'

export function useAsync() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const run = useCallback(async (action) => {
    setLoading(true); setError('')
    try { return await action() }
    catch (err) { setError(apiError(err)); throw err }
    finally { setLoading(false) }
  }, [])
  return { loading, error, setError, run }
}
