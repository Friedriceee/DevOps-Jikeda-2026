import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'

const storage = {
  values: new Map(),
  getItem(key) {
    return this.values.has(key) ? this.values.get(key) : null
  },
  setItem(key, value) {
    this.values.set(key, String(value))
  },
  removeItem(key) {
    this.values.delete(key)
  },
  clear() {
    this.values.clear()
  },
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    Object.defineProperty(window, 'localStorage', { value: storage, configurable: true })
    storage.clear()
  })

  it('stores the session without exposing a password', () => {
    const store = useAuthStore()

    store.setSession({
      accessToken: 'jwt-token',
      userId: 7,
      role: 'Customer',
      profileId: 12,
      displayName: 'Alice',
      password: 'should-not-be-stored',
    })

    expect(store.isAuthenticated).toBe(true)
    expect(store.token).toBe('jwt-token')
    expect(store.user).toEqual({
      id: 7,
      role: 'Customer',
      profileId: 12,
      displayName: 'Alice',
    })
    expect(window.localStorage.getItem('takeout.auth.session')).not.toContain('password')
  })

  it('restores and clears a persisted session', () => {
    window.localStorage.setItem('takeout.auth.session', JSON.stringify({
      accessToken: 'saved-token',
      user: { id: 8, role: 'Customer', profileId: 13 },
    }))

    const store = useAuthStore()
    store.restoreSession()

    expect(store.isAuthenticated).toBe(true)
    expect(store.hasRole('Customer')).toBe(true)

    store.clearSession()
    expect(store.isAuthenticated).toBe(false)
    expect(window.localStorage.getItem('takeout.auth.session')).toBeNull()
  })
})
