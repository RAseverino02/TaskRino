import { useEffect, useState } from 'react'
import { MailPlus, Trash2, UsersRound } from 'lucide-react'
import { Modal } from './Modal'
import { ErrorMessage } from './Feedback'
import { api, apiError } from '../services/api'
import { Avatar } from './Avatar'

export function MembersModal({ open, project, onClose, onUpdated }) {
  const [members, setMembers] = useState(project.members)
  const [form, setForm] = useState({ email: '', role: 'Editor' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const isOwner = project.role === 'Owner'
  useEffect(() => setMembers(project.members), [project.members, open])
  const invite = async (event) => {
    event.preventDefault(); setSaving(true); setError('')
    try { const { data } = await api.post(`/proyectos/${project.id}/miembros`, form); setMembers((current) => [...current, data]); setForm({ email: '', role: 'Editor' }); await onUpdated() }
    catch (err) { setError(apiError(err)) } finally { setSaving(false) }
  }
  const changeRole = async (userId, role) => {
    setError('')
    try { const { data } = await api.patch(`/proyectos/${project.id}/miembros/${userId}`, { role }); setMembers((current) => current.map((member) => member.userId === userId ? data : member)); await onUpdated() }
    catch (err) { setError(apiError(err)) }
  }
  const remove = async (member) => {
    if (!window.confirm(`¿Remover a ${member.name} del proyecto? Sus tareas quedarán sin asignar.`)) return
    setError('')
    try { await api.delete(`/proyectos/${project.id}/miembros/${member.userId}`); setMembers((current) => current.filter((item) => item.userId !== member.userId)); await onUpdated() }
    catch (err) { setError(apiError(err)) }
  }
  return <Modal open={open} onClose={onClose} title="Miembros del proyecto">
    <div className="members-intro"><UsersRound size={21} /><p>{isOwner ? 'Invita personas registradas y define su nivel de acceso.' : 'Estas son las personas que colaboran en el proyecto.'}</p></div>
    {isOwner && <form className="invite-form" onSubmit={invite}><label>Correo electrónico<input type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="persona@correo.com" /></label><label>Rol<select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option value="Editor">Editor</option><option value="Viewer">Solo lectura</option></select></label><button className="button button-primary" disabled={saving}><MailPlus size={17} /> {saving ? 'Invitando…' : 'Invitar'}</button></form>}
    <ErrorMessage message={error} />
    <div className="member-list">{members.map((member) => <div className="member-row" key={member.userId}><Avatar name={member.name} imageUrl={member.profileImageUrl} /><div className="member-info"><strong>{member.name}</strong><span>{member.email}</span></div>{member.role === 'Owner' ? <span className="role-pill">Propietario</span> : isOwner ? <><select aria-label={`Rol de ${member.name}`} value={member.role} onChange={(e) => changeRole(member.userId, e.target.value)}><option value="Editor">Editor</option><option value="Viewer">Solo lectura</option></select><button className="icon-button danger" onClick={() => remove(member)} aria-label={`Remover a ${member.name}`}><Trash2 size={17} /></button></> : <span className="role-pill">{member.role === 'Editor' ? 'Editor' : 'Solo lectura'}</span>}</div>)}</div>
  </Modal>
}
