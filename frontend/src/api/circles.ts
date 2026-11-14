import { api } from './client'

export type CircleRole = 'Owner' | 'Moderator' | 'Member'

export type Circle = {
  id: number
  name: string
  description: string
  createdByUserId: string
  createdAt: string
  isPrivate: boolean
  role?: CircleRole
}

export type CreateCircleRequest = {
  name: string
  description?: string
  isPrivate: boolean
}

export function listMyCircles() {
  return api<Circle[]>('/groups/mine')
}

export function listAvailableCircles() {
  return api<Circle[]>('/groups/available')
}

export function createCircle(body: CreateCircleRequest) {
  return api<Circle>('/groups', {
    method: 'POST',
    body,
  })
}

export function joinCircle(circleId: number) {
  return api<{ circleId: number; userId: string; role: CircleRole }>(`/groups/${circleId}/members`, {
    method: 'POST',
  })
}

export function leaveCircle(circleId: number) {
  return api<{ message: string }>(`/groups/${circleId}/leave`, {
    method: 'POST',
  })
}
