export type MockResult = {
  status: number
  body?: unknown
}

export type MockHandler = (body?: unknown, params?: Record<string, string>) => MockResult

type MockUser = {
  email: string
  password: string
}

type CircleRole = 'Owner' | 'Member'

type CatalogCircle = {
  id: number
  name: string
  description: string
  isPrivate: boolean
  createdByUserId: string
  createdAt: string
}

type Membership = {
  circleId: number
  role: CircleRole
}

const users = new Map<string, MockUser>([
  ['ada@example.com', { email: 'ada@example.com', password: 'password1' }],
])

let currentEmail: string | null = null

const catalog: CatalogCircle[] = [
  { id: 1, name: 'Weekend Hangouts', description: '', isPrivate: true, createdByUserId: 'ada', createdAt: '2026-10-01T08:00:00.000Z' },
  { id: 2, name: 'Open Study Circle', description: '', isPrivate: false, createdByUserId: 'ada', createdAt: '2026-10-02T08:00:00.000Z' },
  { id: 3, name: 'Park Playdates', description: '', isPrivate: false, createdByUserId: 'ada', createdAt: '2026-10-03T08:00:00.000Z' },
  { id: 4, name: 'Toddler Music Hour', description: '', isPrivate: false, createdByUserId: 'ada', createdAt: '2026-10-04T08:00:00.000Z' },
  { id: 5, name: 'Family Reunion', description: '', isPrivate: true, createdByUserId: 'ada', createdAt: '2026-10-05T08:00:00.000Z' },
]

let memberships: Membership[] = [
  { circleId: 1, role: 'Owner' },
  { circleId: 2, role: 'Member' },
]

type StoredPost = {
  id: number
  circleId: number
  message: string
  userId: string
  createdAt: string
}

let postSeq = 2
let circleSeq = 6

let posts: StoredPost[] = [
  {
    id: 1,
    circleId: 1,
    userId: 'ada@example.com',
    message: 'Park meetup Saturday at 10?',
    createdAt: '2026-10-06T08:00:00.000Z',
  },
]

function asRecord(body: unknown) {
  return (body ?? {}) as Record<string, unknown>
}

function circleIdFrom(params?: Record<string, string>) {
  return Number(params?.id)
}

function membershipFor(circleId: number) {
  return memberships.find((item) => item.circleId === circleId)
}

function toGroup(circle: CatalogCircle, role?: CircleRole) {
  return {
    id: circle.id,
    name: circle.name,
    description: circle.description,
    createdByUserId: circle.createdByUserId,
    createdAt: circle.createdAt,
    isPrivate: circle.isPrivate,
    ...(role ? { role } : {}),
  }
}

function myCircles() {
  return memberships.flatMap((membership) => {
    const circle = catalog.find((item) => item.id === membership.circleId)
    if (!circle) {
      return []
    }
    return [toGroup(circle, membership.role)]
  })
}

function availableCircles() {
  const memberIds = new Set(memberships.map((item) => item.circleId))
  return catalog.filter((circle) => !memberIds.has(circle.id)).map((circle) => toGroup(circle))
}

export const handlers: Record<string, MockHandler> = {
  'POST /auth/register'(body) {
    const record = asRecord(body)
    const email = String(record.email ?? '')
    const password = String(record.password ?? '')
    if (!email || !password) {
      return { status: 400, body: { message: 'Email and password are required' } }
    }
    if (users.has(email)) {
      return { status: 400, body: { message: 'user not registered', errors: ['Email is already taken.'] } }
    }
    users.set(email, { email, password })
    return { status: 200, body: { message: 'user registered' } }
  },

  'POST /auth/login'(body) {
    const record = asRecord(body)
    const email = String(record.email ?? '')
    const password = String(record.password ?? '')
    if (!email || !password) {
      return { status: 400, body: { message: 'Email and password are required' } }
    }
    const user = users.get(email)
    if (!user || user.password !== password) {
      return { status: 401, body: { message: 'Invalid email or password' } }
    }
    currentEmail = email
    return { status: 200, body: { message: 'user logged in' } }
  },

  'GET /auth/me'() {
    if (!currentEmail) {
      return { status: 401, body: { message: 'Not logged in' } }
    }
    return { status: 200, body: { email: currentEmail } }
  },

  'POST /auth/logout'() {
    if (!currentEmail) {
      return { status: 401, body: { message: 'Not logged in' } }
    }
    currentEmail = null
    return { status: 200, body: { message: 'user logged out' } }
  },

  'GET /groups/mine'() {
    return { status: 200, body: myCircles() }
  },

  'GET /groups/available'() {
    return { status: 200, body: availableCircles() }
  },

  'POST /groups'(body) {
    const record = asRecord(body)
    const name = String(record.name ?? '').trim()
    if (!name) {
      return { status: 400, body: { message: 'Name is required' } }
    }
    const circle: CatalogCircle = {
      id: circleSeq++,
      name,
      description: String(record.description ?? '').trim(),
      isPrivate: Boolean(record.isPrivate),
      createdByUserId: currentEmail ?? 'ada@example.com',
      createdAt: new Date().toISOString(),
    }
    catalog.push(circle)
    memberships = [...memberships, { circleId: circle.id, role: 'Owner' }]
    return { status: 200, body: toGroup(circle, 'Owner') }
  },

  'POST /groups/:id/members'(_body, params) {
    const circleId = circleIdFrom(params)
    const circle = catalog.find((item) => item.id === circleId)
    if (!circle) {
      return { status: 404, body: { message: 'Group not found' } }
    }
    if (memberships.some((item) => item.circleId === circleId)) {
      return { status: 409, body: { message: 'Already a member' } }
    }
    memberships = [...memberships, { circleId, role: 'Member' }]
    return { status: 200, body: { circleId, userId: currentEmail ?? 'ada@example.com', role: 'Member' } }
  },

  'POST /groups/:id/leave'(_body, params) {
    const circleId = circleIdFrom(params)
    const membership = membershipFor(circleId)
    if (!membership) {
      return { status: 404, body: { message: 'Group not found' } }
    }
    if (membership.role === 'Owner') {
      return { status: 400, body: { message: 'Owners cannot leave their circle' } }
    }
    memberships = memberships.filter((item) => item.circleId !== circleId)
    return { status: 200, body: { message: 'left circle' } }
  },

  'GET /groups/:id/posts'(_body, params) {
    const circleId = circleIdFrom(params)
    if (!membershipFor(circleId)) {
      return { status: 403, body: { message: 'Not a member' } }
    }
    const circlePosts = posts
      .filter((post) => post.circleId === circleId)
      .slice()
      .sort((a, b) => a.id - b.id)
    return { status: 200, body: circlePosts }
  },

  'POST /groups/:id/posts'(body, params) {
    const circleId = circleIdFrom(params)
    if (!membershipFor(circleId)) {
      return { status: 403, body: { message: 'Not a member' } }
    }
    const message = String(asRecord(body).message ?? '').trim()
    if (!message) {
      return { status: 400, body: { message: 'Message is required' } }
    }
    const post: StoredPost = {
      id: postSeq++,
      circleId,
      userId: currentEmail ?? 'ada@example.com',
      message,
      createdAt: new Date().toISOString(),
    }
    posts = [...posts, post]
    return { status: 200, body: post }
  },
}
