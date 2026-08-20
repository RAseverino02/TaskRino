import { useEffect, useRef, useState } from 'react'
import { AlignLeft, CalendarDays, CheckSquare, Clock3, Download, MessageCircle, Paperclip, Send, Target, Trash2, Upload, UserRound, UserRoundCheck } from 'lucide-react'
import { Modal } from './Modal'
import { ErrorMessage } from './Feedback'
import { api, apiError } from '../services/api'
import { useAuth } from '../context/AuthContext'
import { Avatar } from './Avatar'

const blankForm = { title: '', description: '', status: 'ToDo', priority: 'Media', dueDate: '', assignedToId: '' }
const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'application/pdf']

export function TaskModal({ open, task, project, onClose, onSaved, onDeleted }) {
  const { user } = useAuth()
  const [currentTask, setCurrentTask] = useState(task)
  const [form, setForm] = useState(blankForm)
  const [comment, setComment] = useState('')
  const [pendingFiles, setPendingFiles] = useState([])
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const fileInput = useRef(null)
  const editable = project.role === 'Owner' || project.role === 'Editor'

  useEffect(() => {
    if (!open) return
    setCurrentTask(task)
    setForm(task ? {
      title: task.title, description: task.description || '', status: task.status, priority: task.priority,
      dueDate: task.dueDate?.slice(0, 10) || '', assignedToId: task.assignedToId || '',
    } : blankForm)
    setComment(''); setPendingFiles([]); setError('')
  }, [open, task])

  const updateTaskState = (updated) => { setCurrentTask(updated); setForm((old) => ({ ...old, status: updated.status })); onSaved(updated) }
  const uploadFile = async (taskId, file) => {
    const body = new FormData(); body.append('file', file)
    const { data } = await api.post(`/tareas/${taskId}/adjuntos`, body, { headers: { 'Content-Type': 'multipart/form-data' } })
    return data
  }
  const submit = async (event) => {
    event.preventDefault(); setSaving(true); setError('')
    const payload = { ...form, assignedToId: form.assignedToId || null, dueDate: form.dueDate ? new Date(`${form.dueDate}T23:59:00`).toISOString() : null }
    try {
      const { data } = currentTask ? await api.put(`/tareas/${currentTask.id}`, payload) : await api.post(`/proyectos/${project.id}/tareas`, payload)
      updateTaskState(data)

      if (!currentTask) {
        let createdTask = data
        if (comment.trim()) {
          const response = await api.post(`/tareas/${data.id}/comentarios`, { content: comment.trim() })
          createdTask = { ...createdTask, comments: [...(createdTask.comments || []), response.data] }
          setComment('')
          updateTaskState(createdTask)
        }
        for (const file of pendingFiles) {
          const attachment = await uploadFile(data.id, file)
          createdTask = { ...createdTask, attachments: [attachment, ...(createdTask.attachments || [])] }
          setPendingFiles((files) => files.filter((item) => item !== file))
          updateTaskState(createdTask)
        }
        onClose()
      }
    } catch (err) { setError(apiError(err)) } finally { setSaving(false) }
  }
  const removeTask = async () => {
    if (!window.confirm(`¿Eliminar “${currentTask.title}”? Esta acción no se puede deshacer.`)) return
    setSaving(true); setError('')
    try { await api.delete(`/tareas/${currentTask.id}`); onDeleted(currentTask.id) }
    catch (err) { setError(apiError(err)) } finally { setSaving(false) }
  }
  const addComment = async (event) => {
    event.preventDefault(); if (!comment.trim()) return
    try { const { data } = await api.post(`/tareas/${currentTask.id}/comentarios`, { content: comment }); updateTaskState({ ...currentTask, comments: [...currentTask.comments, data] }); setComment('') }
    catch (err) { setError(apiError(err)) }
  }
  const removeComment = async (id) => {
    if (!window.confirm('¿Eliminar este comentario?')) return
    try { await api.delete(`/comentarios/${id}`); updateTaskState({ ...currentTask, comments: currentTask.comments.filter((item) => item.id !== id) }) }
    catch (err) { setError(apiError(err)) }
  }
  const upload = async (event) => {
    const file = event.target.files?.[0]; event.target.value = ''
    if (!file) return
    if (file.size > 5 * 1024 * 1024) return setError('El archivo supera el máximo permitido de 5 MB.')
    if (!allowedTypes.includes(file.type)) return setError('Solo puedes subir imágenes o archivos PDF.')
    if (!currentTask) {
      setPendingFiles((files) => [...files, file])
      setError('')
      return
    }
    setUploading(true); setError('')
    try { const data = await uploadFile(currentTask.id, file); updateTaskState({ ...currentTask, attachments: [data, ...currentTask.attachments] }) }
    catch (err) { setError(apiError(err)) } finally { setUploading(false) }
  }
  const download = async (attachment) => {
    try {
      const { data } = await api.get(`/adjuntos/${attachment.id}`, { responseType: 'blob' })
      const url = URL.createObjectURL(data); const anchor = document.createElement('a'); anchor.href = url; anchor.download = attachment.fileName; anchor.click(); URL.revokeObjectURL(url)
    } catch (err) { setError(apiError(err, 'No pudimos descargar el archivo.')) }
  }
  const removeAttachment = async (id) => {
    if (!window.confirm('¿Eliminar este archivo adjunto?')) return
    try { await api.delete(`/adjuntos/${id}`); updateTaskState({ ...currentTask, attachments: currentTask.attachments.filter((item) => item.id !== id) }) }
    catch (err) { setError(apiError(err)) }
  }

  const assignedMember = project.members.find((member) => member.userId === form.assignedToId)
  const taskForm = <form id="task-editor-form" className="task-form form-stack" onSubmit={submit}>
    <label><span className="label-with-icon"><CheckSquare size={14} /> Título</span><input required minLength="2" maxLength="160" disabled={!editable} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder="¿Qué hay que hacer?" /></label>
    <label><span className="label-with-icon"><AlignLeft size={14} /> Descripción</span><textarea rows="5" maxLength="4000" disabled={!editable} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Agrega contexto, requisitos o enlaces útiles…" /></label>
    <div className="form-grid-2"><label><span className="label-with-icon">Estado <Clock3 size={14} /></span><select disabled={!editable} value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><option value="ToDo">Por hacer</option><option value="InProgress">En progreso</option><option value="Done">Completado</option></select></label><label>Prioridad<select disabled={!editable} value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}><option value="Baja">Baja</option><option value="Media">Media</option><option value="Alta">Alta</option></select></label></div>
    <div className="form-grid-2"><label><span className="label-with-icon"><CalendarDays size={15} /> Fecha de vencimiento</span><input type="date" disabled={!editable} value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} /></label><label><span className="label-with-icon"><UserRound size={15} /> Responsable</span><div className="task-assignee-select">{assignedMember ? <Avatar className="task-assignee-avatar" name={assignedMember.name} imageUrl={assignedMember.profileImageUrl} /> : <img src="/taskrino-mark.png" alt="" aria-hidden="true" />}<select disabled={!editable} value={form.assignedToId} onChange={(e) => setForm({ ...form, assignedToId: e.target.value })}><option value="">Sin asignar</option>{project.members.map((member) => <option value={member.userId} key={member.userId}>{member.name}</option>)}</select></div></label></div>
  </form>

  const comments = currentTask?.comments || []
  const attachments = currentTask?.attachments || []
  const activity = <aside className="task-activity task-activity-left"><section><div className="activity-heading"><h3><MessageCircle size={18} /> Comentarios</h3><span>{currentTask ? comments.length : comment.trim() ? 1 : 0}</span></div>
    {currentTask && <div className="comment-list">{comments.length === 0 && <p className="muted small">Aún no hay comentarios.</p>}{comments.map((item) => <article className="comment" key={item.id}><Avatar className="mini-avatar" name={item.userName} imageUrl={item.userProfileImageUrl} /><div><header><strong>{item.userName}</strong><time>{relativeDate(item.createdAt)}</time>{item.userId === user.id && <button onClick={() => removeComment(item.id)} aria-label="Eliminar comentario"><Trash2 size={14} /></button>}</header><p>{item.content}</p></div></article>)}</div>}
    {editable && (currentTask ? <form className="comment-form" onSubmit={addComment}><textarea rows="2" maxLength="2000" value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Escribe un comentario…" aria-label="Nuevo comentario" /><button className="icon-button primary" disabled={!comment.trim()} aria-label="Publicar comentario"><Send size={17} /></button></form> : <div className="draft-comment"><textarea rows="3" maxLength="2000" value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Escribe el primer comentario…" aria-label="Primer comentario" /><small>Se publicará automáticamente al crear la tarea.</small></div>)}</section>
    <section><div className="activity-heading"><h3><Paperclip size={18} /> Adjuntos</h3><span>{currentTask ? attachments.length : pendingFiles.length}</span></div><div className="attachment-list">{currentTask && attachments.length === 0 && <p className="muted small">No hay archivos adjuntos.</p>}{!currentTask && pendingFiles.length === 0 && <p className="muted small">Aún no has seleccionado archivos.</p>}{attachments.map((item) => <div className="attachment-row" key={item.id}><span className="file-icon">{item.contentType === 'application/pdf' ? 'PDF' : 'IMG'}</span><div><strong title={item.fileName}>{item.fileName}</strong><span>{formatBytes(item.sizeBytes)}</span></div><button className="icon-button" onClick={() => download(item)} aria-label={`Descargar ${item.fileName}`}><Download size={16} /></button>{editable && <button className="icon-button danger" onClick={() => removeAttachment(item.id)} aria-label={`Eliminar ${item.fileName}`}><Trash2 size={16} /></button>}</div>)}{!currentTask && pendingFiles.map((file, index) => <div className="attachment-row pending-attachment" key={`${file.name}-${file.lastModified}-${index}`}><span className="file-icon">{file.type === 'application/pdf' ? 'PDF' : 'IMG'}</span><div><strong title={file.name}>{file.name}</strong><span>{formatBytes(file.size)} · Pendiente</span></div><button type="button" className="icon-button danger" onClick={() => setPendingFiles((files) => files.filter((_, fileIndex) => fileIndex !== index))} aria-label={`Quitar ${file.name}`}><Trash2 size={16} /></button></div>)}</div>{editable && <><input ref={fileInput} type="file" hidden accept="image/jpeg,image/png,image/gif,image/webp,application/pdf" onChange={upload} /><button type="button" className="upload-button" onClick={() => fileInput.current?.click()} disabled={uploading}><Upload size={17} /> {uploading ? 'Subiendo…' : currentTask ? 'Adjuntar imagen o PDF' : 'Seleccionar imagen o PDF'}<small>Máximo 5 MB</small></button></>}</section>
  </aside>

  const actions = editable && <div className="modal-actions modal-actions-spread">{currentTask ? <button type="button" className="button button-danger-ghost" onClick={removeTask} disabled={saving}><Trash2 size={17} /> Eliminar</button> : <span />}<div><button type="button" className="button button-ghost" onClick={onClose}>Cancelar</button><button type="submit" form="task-editor-form" className="button button-primary" disabled={saving}>{saving ? 'Guardando…' : currentTask ? 'Guardar cambios' : 'Crear tarea'}</button></div></div>

  return <Modal open={open} onClose={onClose} title={currentTask ? currentTask.title : 'Nueva tarea'} wide className="task-create-modal" sidePanel={<TaskCreationSummary />}>
    <div className="task-left-stack">{taskForm}{activity}<ErrorMessage message={error} />{actions}</div>
  </Modal>
}

function TaskCreationSummary() {
  return <aside className="task-create-summary" aria-label="Rino Resumen">
    <div className="task-summary-brand"><img src="/taskrino-mark.png" alt="" aria-hidden="true" /><strong>Rino<br />Resumen</strong></div>
    <div className="task-summary-intro"><strong>Impulsando tus proyectos.</strong><p>Rino Tasks te ayuda a organizar y<br />ejecutar con potencia.</p></div>
    <div className="task-progress" aria-label="Progreso inicial: cero por ciento"><span>0%</span><div><img src="/taskrino-mark.png" alt="" aria-hidden="true" /></div></div>
    <div className="task-summary-steps"><h3>Pasos Clave</h3><ul><li><Target size={19} /> Define objetivos</li><li><UserRoundCheck size={19} /> Asigna roles</li><li><CalendarDays size={19} /> Plazos claros</li></ul></div>
  </aside>
}

const formatBytes = (bytes) => bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`
const relativeDate = (date) => new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(date))
