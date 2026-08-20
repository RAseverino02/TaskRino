import { ArrowLeft } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Brand } from '../components/Brand'

export function NotFoundPage() {
  return <main className="not-found">
    <Brand />
    <strong>404</strong>
    <h1>Esta carga tomó otro camino</h1>
    <p>El enlace puede haber cambiado o el recurso ya no está disponible.</p>
    <Link className="button button-primary" to="/"><ArrowLeft size={18} /> Volver a proyectos</Link>
  </main>
}
