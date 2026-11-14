import { api } from './client'

export type Post = {
  id: number
  circleId: number
  message: string
  userId: string
  createdAt: string
}

export function listPosts(circleId: number) {
  return api<Post[]>(`/groups/${circleId}/posts`)
}

export function createPost(circleId: number, message: string) {
  return api<Post>(`/groups/${circleId}/posts`, {
    method: 'POST',
    body: { message },
  })
}
