// Controls the user lacks permission for stay visible and clickable (rather than
// hidden) so the lockdown is something you can see happen: clicking one fires a
// real request and the server really returns 403 - this isn't a client-side
// simulation of denial, it's the actual server enforcement.
export function lockedTitle(allowed: boolean, permission: string) {
  return allowed
    ? undefined
    : `Your account lacks the ${permission} permission - this will be blocked by the server (403)`
}

export function classNames(...classes: Array<string | false | undefined>) {
  return classes.filter(Boolean).join(' ')
}

export const pageShellClass = 'mx-auto max-w-[1080px] px-4 py-8'

export const hintClass = 'mb-2 text-sm text-gray-600'

export const thClass = 'border-b border-gray-100 px-3 py-2.5 text-left'
export const tdClass = 'border-b border-gray-100 px-3 py-2.5'

const buttonBase =
  'rounded-md px-3 py-1.5 text-sm font-medium transition-colors disabled:cursor-default disabled:opacity-60'

export function buttonClass(locked: boolean, variant: 'primary' | 'secondary' = 'primary') {
  if (locked) {
    return classNames(buttonBase, 'bg-gray-200 text-gray-600 hover:bg-gray-300')
  }
  return classNames(
    buttonBase,
    variant === 'secondary' ? 'bg-gray-500 text-white hover:bg-gray-600' : 'bg-blue-600 text-white hover:bg-blue-700',
  )
}

export const inputClass =
  'rounded-md border border-gray-300 px-2.5 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500'

export function navLinkClass(isActive: boolean) {
  return classNames(
    'border-b-2 pb-1 text-sm no-underline',
    isActive ? 'border-blue-600 font-semibold text-blue-600' : 'border-transparent text-gray-600 hover:text-gray-900',
  )
}

export function bannerClass(kind: 'locked' | 'error') {
  return classNames(
    'mb-4 rounded-lg border px-3.5 py-2.5 text-sm',
    kind === 'locked' ? 'border-amber-200 bg-amber-50 text-amber-800' : 'border-red-200 bg-red-50 text-red-700',
  )
}
