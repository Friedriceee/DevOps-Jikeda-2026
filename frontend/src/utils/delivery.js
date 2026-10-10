/**
 * 骑手配送相关的纯函数工具，便于单元测试。
 * 与后端 DeliveryStatus（Assigned/PickedUp/Delivering/Completed）对应。
 */

// Backend RiderDeliveryResponse.Status is the enum ToString()
const STATUS_TEXT = {
  Assigned: 'Assigned',
  PickedUp: 'Picked up',
  Delivering: 'Out for delivery',
  Completed: 'Completed',
}

// Current status -> next executable action (for the backend advance call) + button label
const NEXT_ACTION = {
  Assigned: { action: 'PickUp', label: 'Confirm pickup' },
  PickedUp: { action: 'StartDelivering', label: 'Start delivery' },
  Delivering: { action: 'Complete', label: 'Mark delivered' },
}

/**
 * Delivery status display label.
 * @param {string} status
 * @returns {string}
 */
export function deliveryStatusText(status) {
  return STATUS_TEXT[status] ?? 'Unknown status'
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
