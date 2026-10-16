import http from './http'

export const listAdminAccounts = (params = {}) => http.get('/admin/accounts', { params })
export const setAdminAccountStatus = (id, isActive) => http.put(`/admin/accounts/${id}/status`, { isActive })
export const setAdminAccountRole = (id, role) => http.put(`/admin/accounts/${id}/role`, { role })

export const listAdminCoupons = () => http.get('/admin/coupons')
export const createAdminCoupon = (coupon) => http.post('/admin/coupons', coupon)
export const updateAdminCoupon = (id, coupon) => http.put(`/admin/coupons/${id}`, coupon)
export const setAdminCouponStatus = (id, isActive) => http.put(`/admin/coupons/${id}/status`, { isActive })
export const listCouponAllocations = () => http.get('/admin/coupon-allocations')
export const issueCoupon = (id, userId, quantity) => http.post(`/admin/coupons/${id}/issue`, { userId, quantity })

export const listAdminOrders = (params = {}) => http.get('/admin/orders', { params })
export const listAuditLogs = (params = {}) => http.get('/admin/audit-logs', { params })
