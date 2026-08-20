import { UserRound } from 'lucide-react'
import { apiAssetUrl } from '../services/api'

export function Avatar({ name = '', imageUrl = null, className = 'avatar', fallbackIcon = false }) {
  const source = apiAssetUrl(imageUrl)
  return <span className={className} aria-label={source ? `Foto de ${name}` : undefined}>
    {source ? <img src={source} alt="" /> : fallbackIcon && !name ? <UserRound size={15} /> : initials(name)}
  </span>
}

const initials = (name) => name.split(' ').filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase() || '?'
