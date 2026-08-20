import { useState } from 'react'
import { ArrowRight, Eye, EyeOff, LockKeyhole, Mail } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Brand } from '../components/Brand'
import { ErrorMessage } from '../components/Feedback'
import { useAuth } from '../context/AuthContext'
import { apiError } from '../services/api'

const demoAccounts = [
  { name: 'Severino', email: 'severino@rino.com', password: 'S3ver1n0' },
  { name: 'Richard', email: 'richard@rino.com', password: 'R1ch@rd' },
]

export function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '' })
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const { login } = useAuth()
  const navigate = useNavigate()

  const submit = async (event) => {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      await login(form)
      navigate('/')
    } catch (err) {
      setError(apiError(err, 'No fue posible iniciar sesión.'))
    } finally {
      setLoading(false)
    }
  }

  const useDemoAccount = (account) => {
    setForm({ email: account.email, password: account.password })
    setError('')
  }

  return <div className="login-scene">
    <main className="login-stage">
      <section className="login-card" aria-labelledby="login-title">
        <Brand className="login-card-brand" to="/login" />
        <h1 id="login-title">Fuerza para tus proyectos,<br />orden para tus días</h1>

        <form onSubmit={submit} className="form-stack login-form">
          <label>Correo electrónico
            <div className="input-with-icon login-input"><Mail size={18} /><input type="email" autoComplete="email" required value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} placeholder="nombre@empresa.com" /></div>
          </label>
          <label>Contraseña
            <div className="input-with-icon login-input"><LockKeyhole size={18} /><input type={showPassword ? 'text' : 'password'} autoComplete="current-password" required value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} placeholder="••••••••" /><button type="button" className="password-toggle" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}>{showPassword ? <EyeOff size={17} /> : <Eye size={17} />}</button></div>
          </label>
          <ErrorMessage message={error} />
          <button className="button button-primary button-block login-submit" disabled={loading}>{loading ? 'Entrando…' : <>Entrar <ArrowRight size={18} /></>}</button>
        </form>

        <div className="login-demo" aria-label="Acceso de demostración">
          <span>Acceso de demostración</span>
          <div className="login-demo-options">
            {demoAccounts.map((account) => <button type="button" key={account.email} onClick={() => useDemoAccount(account)}><strong>{account.name}</strong><small>{account.email} · {account.password}</small></button>)}
          </div>
        </div>

        <p className="auth-switch">¿No tienes cuenta? <Link to="/registro">Regístrate gratis</Link></p>
      </section>
    </main>
  </div>
}
