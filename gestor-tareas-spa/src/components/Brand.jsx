import { Link } from 'react-router-dom'

export function Brand({ light = false, className = '', to = '/', compact = false }) {
  return <Link className={`brand ${light ? 'brand-light' : ''} ${compact ? 'brand-compact' : ''} ${className}`.trim()} to={to} aria-label="TaskRino — ir al inicio">
    <img className="brand-logo" src="/taskrino-mark.png" alt="" aria-hidden="true" />
    <span className="brand-word"><span>Task</span><strong>Rino</strong></span>
  </Link>
}
