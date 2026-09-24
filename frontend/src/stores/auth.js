import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

const STORAGE_KEY = 'takeout.auth.session'

function readStoredSession() {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref(null)
  const user = ref(null)

  const isAuthenticated = computed(() => Boolean(token.value))
  const role = computed(() => user.value?.role ?? null)

  function setSession(session) {
    const accessToken = session?.accessToken ?? session?.token
    if (!accessToken) {
      throw new Error('登录响应缺少 accessToken')
    }

    token.value = accessToken
    user.value = {
      id: session.userId ?? session.id ?? null,
      role: session.role ?? null,
      profileId: session.profileId ?? null,
      displayName: session.displayName ?? session.username ?? null,
    }

    window.localStorage.setItem(STORAGE_KEY, JSON.stringify({
      accessToken: token.value,
      user: user.value,
    }))
  }

  function restoreSession() {
    const session = readStoredSession()
    if (!session?.accessToken) return

    token.value = session.accessToken
    user.value = session.user ?? null
  }

  function clearSession() {
    token.value = null
    user.value = null
    window.localStorage.removeItem(STORAGE_KEY)
  }

  function hasRole(expectedRole) {
    return role.value === expectedRole
  }

  return {
    token,
    user,
    role,
    isAuthenticated,
    setSession,
    restoreSession,
    clearSession,
    hasRole,
  }
})
