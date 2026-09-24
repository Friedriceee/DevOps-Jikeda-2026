export function authGuard(authStore, to) {
  if (to.name === 'login' && authStore.isAuthenticated) {
    return { name: 'customer-merchants' }
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return {
      name: 'login',
      query: { redirect: to.fullPath },
    }
  }

  if (to.meta.role && !authStore.hasRole(to.meta.role)) {
    return { name: 'home' }
  }

  return true
}
