const FORMATO_DATA_HORA = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
const FORMATO_HORA = new Intl.DateTimeFormat('pt-BR', { timeStyle: 'short' })
const FORMATO_DIA = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })

/** A API sempre fala UTC; a tela sempre mostra no fuso do navegador. */
export const formatarDataHora = (iso) => (iso ? FORMATO_DATA_HORA.format(new Date(iso)) : '—')
export const formatarHora = (iso) => (iso ? FORMATO_HORA.format(new Date(iso)) : '—')
export const formatarDia = (iso) => (iso ? FORMATO_DIA.format(new Date(iso)) : '—')

export const formatarData = (iso) =>
  iso ? new Date(iso + (iso.length === 10 ? 'T00:00:00' : '')).toLocaleDateString('pt-BR') : '—'

/** Segunda-feira da semana da data informada, 00:00 local. */
export function inicioDaSemana(data) {
  const d = new Date(data)
  const diaSemana = d.getDay()
  const ajuste = diaSemana === 0 ? -6 : 1 - diaSemana
  d.setDate(d.getDate() + ajuste)
  d.setHours(0, 0, 0, 0)
  return d
}

export function somarDias(data, dias) {
  const d = new Date(data)
  d.setDate(d.getDate() + dias)
  return d
}

export const SEVERIDADE_STATUS = {
  Pendente: 'warn',
  Confirmado: 'success',
  Concluido: 'info',
  Cancelado: 'danger',
  Falta: 'contrast',
}
