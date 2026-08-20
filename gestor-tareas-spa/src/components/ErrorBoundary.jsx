import React from 'react'
import { AlertTriangle, RefreshCw } from 'lucide-react'

export class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError() { return { hasError: true } }

  componentDidCatch(error, info) {
    if (import.meta.env.DEV) console.error('Error no controlado en la interfaz', error, info)
  }

  render() {
    if (!this.state.hasError) return this.props.children
    return <main className="not-found"><AlertTriangle size={54} color="#0b8f91" /><h1>Algo no salió como esperábamos</h1><p>Tu información está segura. Recarga la aplicación para intentarlo nuevamente.</p><button className="button button-primary" onClick={() => window.location.reload()}><RefreshCw size={18} /> Recargar aplicación</button></main>
  }
}
