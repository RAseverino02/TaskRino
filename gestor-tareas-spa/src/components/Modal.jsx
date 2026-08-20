import { useEffect } from 'react'
import { X } from 'lucide-react'

export function Modal({ open, onClose, title, subtitle = '', children, wide = false, branded = false, className = '', sidePanel = null }) {
  useEffect(() => {
    if (!open) return
    const closeOnEscape = (event) => event.key === 'Escape' && onClose()
    document.addEventListener('keydown', closeOnEscape)
    document.body.classList.add('modal-open')
    return () => { document.removeEventListener('keydown', closeOnEscape); document.body.classList.remove('modal-open') }
  }, [open, onClose])
  if (!open) return null
  return <div className="modal-backdrop" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
    <section className={`modal-panel ${wide ? 'modal-wide' : ''} ${branded ? 'modal-branded' : ''} ${className}`.trim()} role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <header className="modal-header">
        {branded && <img className="modal-brand-logo" src="/taskrino-mark.png" alt="" aria-hidden="true" />}
        <div className="modal-heading"><h2 id="modal-title">{title}</h2>{subtitle && <p>{subtitle}</p>}</div>
        <button className="icon-button" onClick={onClose} aria-label="Cerrar"><X size={22} /></button>
      </header>
      <div className="modal-body">{children}</div>
      {sidePanel}
    </section>
  </div>
}
