/**
 * 骑手配送相关的纯函数工具，便于单元测试。
 * 与后端 DeliveryStatus（Assigned/PickedUp/Delivering/Completed）对应。
 */

// 后端 RiderDeliveryResponse.Status 是枚举 ToString()
const STATUS_TEXT = {
  Assigned: '已接单',
  PickedUp: '已取餐',
  Delivering: '配送中',
  Completed: '已完成',
}

// 当前状态 → 下一步可执行的动作（给后端 advance 用）与按钮文案
const NEXT_ACTION = {
  Assigned: { action: 'PickUp', label: '确认取餐' },
  PickedUp: { action: 'StartDelivering', label: '开始配送' },
  Delivering: { action: 'Complete', label: '确认送达' },
}

/**
 * 配送状态文案。
 * @param {string} status
 * @returns {string}
 */
export function deliveryStatusText(status) {
  return STATUS_TEXT[status] ?? '未知状态'
}

/**
 * 当前状态的下一步动作；已完成或未知返回 null。
 * @param {string} status
 * @returns {{action: string, label: string} | null}
 */
export function nextDeliveryAction(status) {
  return NEXT_ACTION[status] ?? null
}

/**
 * 是否还能继续推进（未完成且有下一步）。
 * @param {string} status
 * @returns {boolean}
 */
export function canAdvance(status) {
  return nextDeliveryAction(status) !== null
}
