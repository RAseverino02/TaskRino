import { useState } from 'react'
import { ArrowRight, BarChart3, LockKeyhole, Mail, Rocket, UserRound, UsersRound } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Brand } from '../components/Brand'
import { ErrorMessage } from '../components/Feedback'
import { useAuth } from '../context/AuthContext'
import { apiError } from '../services/api'

export function RegisterPage() {
  const [form, setForm] = useState({ name: '', email: '', password: '', confirmation: '' })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const { register } = useAuth()
  const navigate = useNavigate()

  const submit = async (event) => {
    event.preventDefault()
    setError('')
    if (form.password !== form.confirmation) return setError('Las contraseñas no coinciden.')
    setLoading(true)
    try {
      await register({ name: form.name, email: form.email, password: form.password })
      navigate('/')
    } catch (err) {
      setError(apiError(err, 'No fue posible crear la cuenta.'))
    } finally {
      setLoading(false)
    }
  }

  return <div className="auth-page">
    <section className="auth-story">
      <Brand light className="auth-brand" />
      <div className="auth-story-copy">
        <h1>Fuerza para tus proyectos,<br />orden para tus días</h1>
        <p>Crea tu espacio TaskRino y convierte cada objetivo en una carga organizada hacia el progreso.</p>
        <ul>
          <li><Rocket /> Progreso con propósito</li>
          <li><BarChart3 /> Tableros claros y visuales</li>
          <li><UsersRound /> Tu equipo en la misma dirección</li>
        </ul>
      </div>
      <p className="auth-quote">Tu próximo proyecto merece comenzar con fuerza.</p>
    </section>

    <main className="auth-form-wrap">
      <div className="auth-form-card">
        <Brand compact className="mobile-brand" />
        <p className="eyebrow">Crea tu espacio</p>
        <h2>Regístrate en TaskRino</h2>
        <p className="muted">Sin tarjeta. Solo necesitas un correo.</p>
        <form onSubmit={submit} className="form-stack">
          <label>Nombre completo<div className="input-with-icon"><UserRound size={18} /><input required minLength="2" maxLength="100" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Richard Alexander Severino Morales" /></div></label>
          <label>Correo electrónico<div className="input-with-icon"><Mail size={18} /><input type="email" required autoComplete="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} placeholder="tu@correo.com" /></div></label>
          <label>Contraseña<div className="input-with-icon"><LockKeyhole size={18} /><input type="password" required minLength="8" autoComplete="new-password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} placeholder="Mínimo 8 caracteres" /></div></label>
          <label>Confirmar contraseña<div className="input-with-icon"><LockKeyhole size={18} /><input type="password" required autoComplete="new-password" value={form.confirmation} onChange={(event) => setForm({ ...form, confirmation: event.target.value })} placeholder="Repite tu contraseña" /></div></label>
          <ErrorMessage message={error} />
          <button className="button button-primary button-block" disabled={loading}>{loading ? 'Creando…' : <>Crear mi cuenta <ArrowRight size={18} /></>}</button>
        </form>
        <p className="auth-switch">¿Ya tienes cuenta? <Link to="/login">Iniciar sesión</Link></p>
      </div>
    </main>
  </div>
}
