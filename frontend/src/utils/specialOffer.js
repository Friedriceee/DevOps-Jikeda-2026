function toNumber(value) {
  const text = String(value ?? '').trim()
  return text === '' ? Number.NaN : Number(text)
}

function hasAtMostTwoDecimals(value) {
  return Number(value.toFixed(2)) === value
}

export function isValidSpecialOffer(minPrice, amountRemission) {
  const threshold = toNumber(minPrice)
  const remission = toNumber(amountRemission)

  return Number.isFinite(threshold)
    && Number.isFinite(remission)
    && threshold > 0
    && remission > 0
    && remission < threshold
    && hasAtMostTwoDecimals(threshold)
    && hasAtMostTwoDecimals(remission)
}

export function buildSpecialOfferPayload(form, merchantId) {
  return {
    merchantId: Number(merchantId),
    minPrice: toNumber(form.minPrice),
    amountRemission: toNumber(form.amountRemission),
  }
}

export function buildSpecialOfferUpdatePayload(form) {
  return {
    minPrice: toNumber(form.minPrice),
    amountRemission: toNumber(form.amountRemission),
  }
}

