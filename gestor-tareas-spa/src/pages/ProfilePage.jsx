import { useRef, useState } from 'react'
import { CalendarDays, Camera, Mail, Save, Trash2, UserRound } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { api, apiError } from '../services/api'
import { ErrorMessage } from '../components/Feedback'
import { Avatar } from '../components/Avatar'

export function ProfilePage() {
  const { user, updateUser } = useAuth()
  const [name, setName] = useState(user?.name || '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)
  const [photoSaving, setPhotoSaving] = useState(false)
  const photoInput = useRef(null)
  const submit = async (event) => {
    event.preventDefault(); setSaving(true); setError(''); setSaved(false)
    try { const { data } = await api.put('/usuarios/me', { name }); updateUser(data); setSaved(true) }
    catch (err) { setError(apiError(err)) } finally { setSaving(false) }
  }
  const uploadPhoto = async (event) => {
    const file = event.target.files?.[0]; event.target.value = ''
    if (!file) return
    if (file.size > 2 * 1024 * 1024) return setError('La foto de perfil no puede superar los 2 MB.')
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) return setError('Utiliza una imagen JPG, PNG o WebP.')
    setPhotoSaving(true); setError(''); setSaved(false)
    const body = new FormData(); body.append('file', file)
    try { const { data } = await api.post('/usuarios/me/foto', body, { headers: { 'Content-Type': 'multipart/form-data' } }); updateUser(data); setSaved(true) }
    catch (err) { setError(apiError(err, 'No pudimos subir la foto de perfil.')) } finally { setPhotoSaving(false) }
  }
  const deletePhoto = async () => {
    if (!window.confirm('¿Quitar tu foto de perfil?')) return
    setPhotoSaving(true); setError(''); setSaved(false)
    try { const { data } = await api.delete('/usuarios/me/foto'); updateUser(data); setSaved(true) }
    catch (err) { setError(apiError(err, 'No pudimos quitar la foto de perfil.')) } finally { setPhotoSaving(false) }
  }
  return <div className="page-container profile-page"><div className="page-title"><p className="eyebrow">Tu cuenta</p><h1>Perfil</h1><p>Administra la información que ven los demás miembros.</p></div>
    <section className="profile-card"><div className="profile-identity"><Avatar className="profile-avatar" name={user?.name} imageUrl={user?.profileImageUrl} /><div><h2>{user?.name}</h2><p>{user?.email}</p></div></div>
      <form className="form-stack" onSubmit={submit}><div className="profile-photo-editor"><Avatar className="profile-photo-preview" name={user?.name} imageUrl={user?.profileImageUrl} /><div><strong>Foto de perfil</strong><p>JPG, PNG o WebP. Máximo 2 MB.</p><div className="profile-photo-actions"><input ref={photoInput} type="file" hidden accept="image/jpeg,image/png,image/webp" onChange={uploadPhoto} /><button type="button" className="button button-secondary" onClick={() => photoInput.current?.click()} disabled={photoSaving}><Camera size={17} /> {photoSaving ? 'Procesando…' : user?.profileImageUrl ? 'Cambiar foto' : 'Subir foto'}</button>{user?.profileImageUrl && <button type="button" className="button button-danger-ghost" onClick={deletePhoto} disabled={photoSaving}><Trash2 size={16} /> Quitar</button>}</div></div></div><label>Nombre completo<div className="input-with-icon"><UserRound size={18} /><input required minLength="2" maxLength="100" value={name} onChange={(e) => setName(e.target.value)} /></div></label>
        <label>Correo electrónico<div className="input-with-icon disabled"><Mail size={18} /><input disabled value={user?.email || ''} /></div><small>El correo se usa para iniciar sesión y recibir invitaciones.</small></label>
        <div className="profile-date"><CalendarDays size={17} /> Miembro desde {new Intl.DateTimeFormat('es', { dateStyle: 'long' }).format(new Date(user?.registeredAt))}</div>
        <ErrorMessage message={error} />{saved && <p className="success-message">Los cambios se guardaron correctamente.</p>}
        <div><button className="button button-primary" disabled={saving}><Save size={17} /> {saving ? 'Guardando…' : 'Guardar cambios'}</button></div>
      </form>
    </section>
  </div>
}
