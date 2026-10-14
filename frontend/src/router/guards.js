export function authGuard(authStore, to) {
  if (to.name === 'login' && authStore.isAuthenticated) {
    if (authStore.role === 'Administrator') return { name: 'admin-dashboard' }
    if (authStore.role === 'Merchant') return { name: 'merchant-dishes' }
    if (authStore.role === 'Rider') return { name: 'rider-deliveries' }
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

export function postLoginDestination(redirect, role) {
  if (typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')) {
    return redirect
  }
  if (role === 'Merchant') return '/merchant/dishes'
  if (role === 'Rider') return '/rider/deliveries'
  if (role === 'Administrator') return '/admin'
  return '/customer/merchants'
}
