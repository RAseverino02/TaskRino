import { useCallback, useEffect, useState } from 'react'
import { ArrowRight, CalendarDays, FolderKanban, Plus, UsersRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { EmptyState, ErrorMessage, Spinner } from '../components/Feedback'
import { Modal } from '../components/Modal'
import { useAuth } from '../context/AuthContext'
import { api, apiError } from '../services/api'

const emptyProject = { name: '', description: '', color: '#1C6E72' }
const projectColors = ['#1C6E72', '#2F5DA8', '#2F9B62', '#F0B429', '#668895', '#D73B37']

export function DashboardPage() {
  const { user } = useAuth()
  const [projects, setProjects] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [creating, setCreating] = useState(false)
  const [saving, setSaving] = useState(false)
  const [form, setForm] = useState(emptyProject)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      setProjects((await api.get('/proyectos')).data)
    } catch (err) {
      setError(apiError(err, 'No pudimos cargar tus proyectos.'))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const createProject = async (event) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      const { data } = await api.post('/proyectos', form)
      setProjects((current) => [{ ...data, taskCount: 0, completedTaskCount: 0 }, ...current])
      setCreating(false)
      setForm(emptyProject)
    } catch (err) {
      setError(apiError(err))
    } finally {
      setSaving(false)
    }
  }

  return <div className="page-container">
    <section className="dashboard-hero">
      <div>
        <p className="eyebrow">Espacio de trabajo</p>
        <h1>{greeting()}, {user?.name?.split(' ')[0]}</h1>
        <p>Estas son las iniciativas que están moviendo tu equipo.</p>
        <strong>Fuerza para tus proyectos, orden para tus días.</strong>
      </div>
      <button className="button button-primary" onClick={() => setCreating(true)}><Plus size={18} /> Nuevo proyecto</button>
    </section>

    <div className="section-heading"><div><h2>Tus proyectos</h2><p>{projects.length} {projects.length === 1 ? 'proyecto activo' : 'proyectos activos'}</p></div></div>
    <ErrorMessage message={error} />
    {loading ? <Spinner label="Cargando proyectos…" /> : projects.length === 0
      ? <EmptyState icon={<FolderKanban size={36} />} title="Tu primer proyecto empieza aquí" text="Crea un espacio para organizar tareas, fechas y responsables." action={<button className="button button-primary" onClick={() => setCreating(true)}><Plus size={18} /> Crear proyecto</button>} />
      : <div className="project-grid">{projects.map((project) => <ProjectCard key={project.id} project={project} />)}</div>}

    <Modal open={creating} onClose={() => setCreating(false)} title="Nuevo proyecto — TaskRino" subtitle="Carga contra el desorden, domina tus tareas." branded>
      <form className="form-stack project-create-form" onSubmit={createProject}>
        <label>Nombre<input required minLength="2" maxLength="120" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Ej. Lanzamiento de la carga de marketing" /></label>
        <label>Descripción<textarea maxLength="1000" rows="4" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="¿Cuál es el objetivo principal de esta carga?" /></label>
        <fieldset className="project-color-fieldset">
          <legend>Color del proyecto</legend>
          <div className="project-swatches">
            {projectColors.map((color) => <button key={color} type="button" className={`color-swatch ${form.color === color ? 'selected' : ''}`} style={{ '--swatch': color }} onClick={() => setForm({ ...form, color })} aria-label={`Seleccionar color ${color}`} aria-pressed={form.color === color} />)}
          </div>
          <span className="selected-color">{form.color.toUpperCase()}</span>
        </fieldset>
        <div className="modal-actions"><button type="button" className="button button-ghost" onClick={() => setCreating(false)}>Cancelar</button><button className="button button-primary" disabled={saving}>{saving ? 'Creando…' : 'Crear proyecto'}</button></div>
      </form>
    </Modal>
  </div>
}

function ProjectCard({ project }) {
  const progress = project.taskCount ? Math.round(project.completedTaskCount / project.taskCount * 100) : 0
  return <Link to={`/proyectos/${project.id}`} className="project-card">
    <div className="project-color" style={{ background: project.color }} />
    <div className="project-card-body">
      <div className="project-card-top"><span className="role-pill">{roleLabel(project.role)}</span><ArrowRight size={18} /></div>
      <h3>{project.name}</h3>
      <p>{project.description || 'Proyecto sin descripción.'}</p>
      <div className="progress-meta"><span>{project.completedTaskCount} de {project.taskCount} tareas</span><strong>{progress}%</strong></div>
      <div className="progress-track"><span style={{ width: `${progress}%`, background: project.color }} /></div>
      <div className="project-card-footer"><span><CalendarDays size={15} /> {new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short' }).format(new Date(project.createdAt))}</span><span><UsersRound size={15} /> {roleLabel(project.role)}</span></div>
    </div>
  </Link>
}

const roleLabel = (role) => ({ Owner: 'Propietario', Editor: 'Editor', Viewer: 'Lector' }[role] || role)

function greeting() {
  const hour = new Date().getHours()
  if (hour < 12) return 'Buenos días'
  if (hour < 19) return 'Buenas tardes'
  return 'Buenas noches'
}
