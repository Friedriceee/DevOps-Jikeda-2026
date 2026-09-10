/**
 * 把数字金额格式化为「¥12.00」。
 * @param {number} value
 * @returns {string}
 */
export function formatYuan(value) {
  const n = Number(value)
  if (!Number.isFinite(n)) return '¥0.00'
  return `¥${n.toFixed(2)}`
}

/**
 * 校验菜品价格：必须是大于 0 的数字。
 * @param {unknown} value
 * @returns {boolean}
 */
export function isValidPrice(value) {
  const n = Number(value)
  return Number.isFinite(n) && n > 0
}
