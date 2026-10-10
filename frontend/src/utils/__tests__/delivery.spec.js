import { describe, expect, it } from 'vitest'
import { canAdvance, deliveryStatusText, nextDeliveryAction } from '@/utils/delivery'

describe('delivery status helpers', () => {
  it('maps delivery status to display text', () => {
    expect(deliveryStatusText('Assigned')).toBe('Assigned')
    expect(deliveryStatusText('PickedUp')).toBe('Picked up')
    expect(deliveryStatusText('Delivering')).toBe('Out for delivery')
    expect(deliveryStatusText('Completed')).toBe('Completed')
    expect(deliveryStatusText('Nope')).toBe('Unknown status')
  })

  it('returns the correct next action per status', () => {
    expect(nextDeliveryAction('Assigned')).toEqual({ action: 'PickUp', label: 'Confirm pickup' })
    expect(nextDeliveryAction('PickedUp')).toEqual({ action: 'StartDelivering', label: 'Start delivery' })
    expect(nextDeliveryAction('Delivering')).toEqual({ action: 'Complete', label: 'Mark delivered' })
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
