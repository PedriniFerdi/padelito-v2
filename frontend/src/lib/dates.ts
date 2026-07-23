export const CLUB_TIME_ZONE = 'America/Argentina/Buenos_Aires'

export function todayInClub(date = new Date()) {
  const parts = new Intl.DateTimeFormat('en', {
    timeZone: CLUB_TIME_ZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(date)
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]))
  return `${value.year}-${value.month}-${value.day}`
}
