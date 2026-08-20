import { AlertCircle, LoaderCircle } from 'lucide-react'

export function Spinner({ label = 'Cargando…' }) {
  return <div className="spinner-state" role="status"><LoaderCircle className="spin" size={24} /><span>{label}</span></div>
}

export function ErrorMessage({ message }) {
  return message ? <div className="error-message" role="alert"><AlertCircle size={17} /><span>{message}</span></div> : null
}

export function EmptyState({ icon, title, text, action }) {
  return <div className="empty-state">{icon}<h3>{title}</h3><p>{text}</p>{action}</div>
}
