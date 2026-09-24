export function authGuard(authStore, to) {
  if (to.name === 'login' && authStore.isAuthenticated) {
    return authStore.role === 'Merchant'
      ? { name: 'merchant-dishes' }
      : { name: 'customer-merchants' }
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

export function postLoginDestination(redirect, role) {
  if (typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')) {
    return redirect
  }
  return role === 'Merchant' ? '/merchant/dishes' : '/customer/merchants'
}
