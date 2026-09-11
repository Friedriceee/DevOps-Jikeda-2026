export function isValidDishName(value) {
  const name = String(value ?? '').trim()
  return name.length >= 1 && name.length <= 50
}

export function isValidInventory(value) {
  return value !== '' && value !== null && value !== undefined
    && Number.isInteger(Number(value)) && Number(value) >= 0
}

export function buildDishPayload(form, merchantId) {
  return {
    merchantId: Number(merchantId),
    name: String(form.name ?? '').trim(),
    price: Number(form.price),
    category: String(form.category ?? '').trim() || null,
    imageUrl: String(form.imageUrl ?? '').trim() || null,
    inventory: Number(form.inventory),
  }
}

export function buildDishUpdatePayload(form) {
  const { merchantId: _merchantId, ...payload } = buildDishPayload(form, 0)
  return payload
}
