import { describe, expect, it } from 'vitest'
import { canAdvance, deliveryStatusText, nextDeliveryAction } from '@/utils/delivery'

describe('delivery status helpers', () => {
  it('maps delivery status to Chinese text', () => {
    expect(deliveryStatusText('Assigned')).toBe('已接单')
    expect(deliveryStatusText('PickedUp')).toBe('已取餐')
    expect(deliveryStatusText('Delivering')).toBe('配送中')
    expect(deliveryStatusText('Completed')).toBe('已完成')
    expect(deliveryStatusText('Nope')).toBe('未知状态')
  })

  it('returns the correct next action per status', () => {
    expect(nextDeliveryAction('Assigned')).toEqual({ action: 'PickUp', label: '确认取餐' })
    expect(nextDeliveryAction('PickedUp')).toEqual({ action: 'StartDelivering', label: '开始配送' })
    expect(nextDeliveryAction('Delivering')).toEqual({ action: 'Complete', label: '确认送达' })
  })

  it('has no next action once completed or for unknown status', () => {
    expect(nextDeliveryAction('Completed')).toBeNull()
    expect(nextDeliveryAction('Nope')).toBeNull()
  })

  it('canAdvance reflects whether a next action exists', () => {
    expect(canAdvance('Assigned')).toBe(true)
    expect(canAdvance('Delivering')).toBe(true)
    expect(canAdvance('Completed')).toBe(false)
  })
})
