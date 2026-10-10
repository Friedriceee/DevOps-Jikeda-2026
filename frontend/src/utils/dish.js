export function isValidDishName(value) {
  const name = String(value ?? '').trim()
  return name.length >= 1 && name.length <= 50
}

export function isValidInventory(value) {
  return value !== '' && value !== null && value !== undefined
    && Number.isInteger(Number(value)) && Number(value) >= 0
}

export function buildDishPayload(form, merchantId) {
  const itemType = form.itemType || 'Standard'
  return {
    merchantId: Number(merchantId),
    name: String(form.name ?? '').trim(),
    price: Number(form.price),
    category: String(form.category ?? '').trim() || null,
    imageUrl: String(form.imageUrl ?? '').trim() || null,
    inventory: Number(form.inventory),
    itemType,
    bundleSize: itemType === 'Bundle' ? Number(form.bundleSize) : null,
    promotionStartsAtUtc: itemType === 'Promotional' ? utcTimestamp(form.promotionStartsAtUtc) : null,
    promotionEndsAtUtc: itemType === 'Promotional' ? utcTimestamp(form.promotionEndsAtUtc) : null,
  }
}

function utcTimestamp(value) {
  const input = typeof value === 'string' && /^\d{4}-\d\d-\d\dT\d\d:\d\d(:\d\d)?$/.test(value) ? `${value}Z` : value
  const date = new Date(input)
  return value && !Number.isNaN(date.getTime()) ? date.toISOString() : null
}

export function menuSettingsError(form) {
  if (!['Standard', 'Bundle', 'Promotional'].includes(form.itemType || 'Standard'))
    return 'Choose Standard, Bundle, or Promotional.'
  if (form.itemType === 'Bundle' && (!Number.isInteger(Number(form.bundleSize)) || Number(form.bundleSize) < 2))
    return 'Bundle size must be a whole number of at least 2.'
  if (form.itemType === 'Promotional') {
    const start = utcTimestamp(form.promotionStartsAtUtc)
    const end = utcTimestamp(form.promotionEndsAtUtc)
    if (!start || !end) return 'Enter both promotion times in UTC.'
    if (end <= start) return 'Promotion end time must be after its start time.'
  }
  return ''
}

export function promotionInput(value) {
  return utcTimestamp(value)?.slice(0, 16) || ''
}

export function buildDishUpdatePayload(form) {
  const { merchantId: _merchantId, ...payload } = buildDishPayload(form, 0)
  return payload
}
