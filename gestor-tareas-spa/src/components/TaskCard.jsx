import { CalendarDays, ChevronLeft, ChevronRight, MessageCircle, Paperclip } from 'lucide-react'
import { Avatar } from './Avatar'

const priorityLabel = { Alta: 'Alta', Media: 'Media', Baja: 'Baja' }
const statusOrder = ['ToDo', 'InProgress', 'Done']

export function TaskCard({ task, editable, onOpen, onMove }) {
  const currentIndex = statusOrder.indexOf(task.status)
  const overdue = task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done'
  return <article className="task-card" draggable={editable} onDragStart={(event) => { event.dataTransfer.setData('text/task-id', task.id); event.dataTransfer.effectAllowed = 'move' }} onClick={onOpen} tabIndex="0" onKeyDown={(event) => (event.key === 'Enter' || event.key === ' ') && onOpen()}>
    <div className="task-card-top"><span className={`priority priority-${task.priority.toLowerCase()}`}>{priorityLabel[task.priority]}</span>{editable && <div className="quick-move" onClick={(event) => event.stopPropagation()}>{currentIndex > 0 && <button onClick={() => onMove(statusOrder[currentIndex - 1])} aria-label="Mover a la columna anterior"><ChevronLeft size={16} /></button>}{currentIndex < 2 && <button onClick={() => onMove(statusOrder[currentIndex + 1])} aria-label="Mover a la columna siguiente"><ChevronRight size={16} /></button>}</div>}</div>
    <h3>{task.title}</h3>{task.description && <p>{task.description}</p>}
    {task.dueDate && <div className={`task-date ${overdue ? 'overdue' : ''}`}><CalendarDays size={14} /> {formatDate(task.dueDate)}{overdue && ' · Vencida'}</div>}
    <footer><span className="assignee"><Avatar className="mini-avatar" name={task.assignedToName} imageUrl={task.assignedToProfileImageUrl} fallbackIcon />{task.assignedToName || 'Sin asignar'}</span><span className="task-signals">{task.comments.length > 0 && <span><MessageCircle size={14} />{task.comments.length}</span>}{task.attachments.length > 0 && <span><Paperclip size={14} />{task.attachments.length}</span>}</span></footer>
  </article>
}

const formatDate = (date) => new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short' }).format(new Date(date))
