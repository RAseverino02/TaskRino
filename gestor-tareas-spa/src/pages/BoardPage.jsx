import { useCallback, useEffect, useMemo, useState } from 'react'
import { ArrowLeft, CheckCircle2, CircleDashed, Filter, ListTodo, Plus, Settings2, UsersRound } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api, apiError } from '../services/api'
import { ErrorMessage, Spinner } from '../components/Feedback'
import { TaskCard } from '../components/TaskCard'
import { TaskModal } from '../components/TaskModal'
import { MembersModal } from '../components/MembersModal'

const columns = [
  { key: 'ToDo', title: 'Por hacer', icon: CircleDashed },
  { key: 'InProgress', title: 'En progreso', icon: ListTodo },
  { key: 'Done', title: 'Completado', icon: CheckCircle2 },
]

export function BoardPage() {
  const { projectId } = useParams()
  const [project, setProject] = useState(null)
  const [tasks, setTasks] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [selectedTask, setSelectedTask] = useState(undefined)
  const [membersOpen, setMembersOpen] = useState(false)
  const [filters, setFilters] = useState({ estado: '', prioridad: '', asignadoA: '', orden: 'estado' })

  const loadProject = useCallback(async () => {
    const { data } = await api.get(`/proyectos/${projectId}`); setProject(data); return data
  }, [projectId])
  const loadTasks = useCallback(async () => {
    const params = { pageSize: 100 }
    if (filters.estado) params.estado = filters.estado
    if (filters.prioridad) params.prioridad = filters.prioridad
    if (filters.asignadoA) params.asignadoA = filters.asignadoA
    params.orden = filters.orden
    const { data } = await api.get(`/proyectos/${projectId}/tareas`, { params }); setTasks(data.items)
  }, [projectId, filters])

  useEffect(() => {
    let active = true; setLoading(true); setError('')
    Promise.all([loadProject(), loadTasks()]).catch((err) => active && setError(apiError(err, 'No pudimos abrir este proyecto.'))).finally(() => active && setLoading(false))
    return () => { active = false }
  }, [loadProject, loadTasks])

  const editable = project?.role === 'Owner' || project?.role === 'Editor'
  const grouped = useMemo(() => Object.fromEntries(columns.map(({ key }) => [key, tasks.filter((task) => task.status === key)])), [tasks])
  const saveTask = (task) => setTasks((current) => current.some((item) => item.id === task.id) ? current.map((item) => item.id === task.id ? task : item) : [task, ...current])
  const deleteTask = (id) => { setTasks((current) => current.filter((task) => task.id !== id)); setSelectedTask(undefined) }
  const moveTask = async (taskId, status) => {
    if (!editable) return
    const original = tasks.find((task) => task.id === taskId)
    if (!original || original.status === status) return
    setTasks((current) => current.map((task) => task.id === taskId ? { ...task, status } : task))
    try { saveTask((await api.patch(`/tareas/${taskId}/estado`, { status })).data) }
    catch (err) { saveTask(original); setError(apiError(err, 'No pudimos mover la tarea.')) }
  }

  if (loading) return <div className="page-container"><Spinner label="Preparando el tablero…" /></div>
  if (!project) return <div className="page-container"><ErrorMessage message={error || 'No encontramos este proyecto.'} /></div>
  return <div className="board-page">
    <header className="board-header"><div className="board-title-row"><Link className="icon-button" to="/" aria-label="Volver"><ArrowLeft size={20} /></Link><span className="project-dot" style={{ background: project.color }} /><div><h1>{project.name}</h1><p>{project.description || 'Sin descripción'}</p></div></div>
      <div className="board-actions"><button className="button button-secondary" onClick={() => setMembersOpen(true)}><UsersRound size={17} /> Miembros <span className="count-badge">{project.members.length}</span></button>{editable && <button className="button button-primary" onClick={() => setSelectedTask(null)}><Plus size={18} /> Nueva tarea</button>}</div>
    </header>
    <section className="filter-bar"><div className="filter-label"><Filter size={17} /> Filtros</div><label><span>Estado</span><select value={filters.estado} onChange={(e) => setFilters({ ...filters, estado: e.target.value })}><option value="">Todos</option><option value="ToDo">Por hacer</option><option value="InProgress">En progreso</option><option value="Done">Completado</option></select></label><label><span>Prioridad</span><select value={filters.prioridad} onChange={(e) => setFilters({ ...filters, prioridad: e.target.value })}><option value="">Todas</option><option value="Alta">Alta</option><option value="Media">Media</option><option value="Baja">Baja</option></select></label><label><span>Asignado</span><select value={filters.asignadoA} onChange={(e) => setFilters({ ...filters, asignadoA: e.target.value })}><option value="">Todos</option>{project.members.map((member) => <option key={member.userId} value={member.userId}>{member.name}</option>)}</select></label><label><span>Orden</span><select value={filters.orden} onChange={(e) => setFilters({ ...filters, orden: e.target.value })}><option value="estado">Estado</option><option value="prioridad">Prioridad</option><option value="vencimiento">Vencimiento</option><option value="recientes">Más recientes</option><option value="titulo">Título</option></select></label>{(filters.estado || filters.prioridad || filters.asignadoA || filters.orden !== 'estado') && <button className="text-button" onClick={() => setFilters({ estado: '', prioridad: '', asignadoA: '', orden: 'estado' })}>Limpiar filtros</button>}<div className="role-indicator"><Settings2 size={15} /> {roleLabel(project.role)}</div></section>
    <ErrorMessage message={error} />
    <div className="kanban-board">{columns.map(({ key, title, icon: Icon }) => <section key={key} className="kanban-column" onDragOver={(event) => editable && event.preventDefault()} onDrop={(event) => moveTask(event.dataTransfer.getData('text/task-id'), key)}>
      <header><span className={`column-icon status-${key}`}><Icon size={17} /></span><h2>{title}</h2><span className="task-count">{grouped[key].length}</span></header>
      <div className="task-list">{grouped[key].map((task) => <TaskCard key={task.id} task={task} editable={editable} onOpen={() => setSelectedTask(task)} onMove={(status) => moveTask(task.id, status)} />)}
        {grouped[key].length === 0 && <div className="column-empty">Suelta una tarea aquí</div>}
        {editable && key === 'ToDo' && <button className="add-task-inline" onClick={() => setSelectedTask(null)}><Plus size={17} /> Agregar tarea</button>}
      </div>
    </section>)}</div>
    <TaskModal open={selectedTask !== undefined} task={selectedTask} project={project} onClose={() => setSelectedTask(undefined)} onSaved={saveTask} onDeleted={deleteTask} />
    <MembersModal open={membersOpen} project={project} onClose={() => setMembersOpen(false)} onUpdated={loadProject} />
  </div>
}

const roleLabel = (role) => ({ Owner: 'Propietario', Editor: 'Editor', Viewer: 'Solo lectura' }[role] || role)
