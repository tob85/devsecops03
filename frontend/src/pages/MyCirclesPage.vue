<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { createCircle, leaveCircle, listMyCircles, type Circle } from '@/api/circles'

const circles = ref<Circle[]>([])
const error = ref('')
const pending = ref(false)
const creating = ref(false)
const name = ref('')
const description = ref('')
const isPrivate = ref(false)

async function loadCircles() {
  error.value = ''
  pending.value = true
  try {
    circles.value = await listMyCircles()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not load circles'
  } finally {
    pending.value = false
  }
}

async function onCreate() {
  error.value = ''
  creating.value = true
  try {
    await createCircle({
      name: name.value,
      description: description.value,
      isPrivate: isPrivate.value,
    })
    name.value = ''
    description.value = ''
    isPrivate.value = false
    await loadCircles()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not create circle'
  } finally {
    creating.value = false
  }
}

async function onLeave(circle: Circle) {
  error.value = ''
  try {
    await leaveCircle(circle.id)
    circles.value = circles.value.filter((item) => item.id !== circle.id)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not leave circle'
  }
}

onMounted(() => {
  void loadCircles()
})
</script>

<template>
  <section>
    <h1>My Circles</h1>
    <p>Your groups for family activities, playdates, and local meetups.</p>

    <form class="create-form" @submit.prevent="onCreate">
      <label for="circle-name">Name</label>
      <input id="circle-name" v-model="name" name="name" required />
      <label for="circle-description">Description</label>
      <input id="circle-description" v-model="description" name="description" />
      <label class="private-check">
        <input v-model="isPrivate" type="checkbox" name="isPrivate" />
        Private
      </label>
      <button type="submit" :disabled="creating">
        {{ creating ? 'Creating…' : 'Create Circle' }}
      </button>
    </form>

    <p v-if="pending">Loading circles…</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <p v-else-if="!pending && !circles.length">You have not joined any circles yet.</p>

    <ul v-if="circles.length" class="circle-list">
      <li
        v-for="circle in circles"
        :key="circle.id"
        :class="{ owner: circle.role === 'Owner' }"
        :data-circle-name="circle.name"
        :data-role="circle.role?.toLowerCase()"
      >
        <div>
          <RouterLink class="circle-name" :to="`/circles/${circle.id}`">{{ circle.name }}</RouterLink>
          <span v-if="circle.role === 'Owner'" class="owner-badge">Owner</span>
          <p class="meta">{{ circle.isPrivate ? 'private' : 'public' }} · {{ circle.role }}</p>
        </div>
        <button
          v-if="circle.role !== 'Owner'"
          type="button"
          :aria-label="`Leave ${circle.name}`"
          @click="onLeave(circle)"
        >
          Leave
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.create-form {
  display: grid;
  gap: 0.5rem;
  margin: 1rem 0 1.5rem;
}

.create-form button,
.private-check {
  justify-self: start;
}

.private-check {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.circle-name {
  font-weight: 700;
  text-decoration: none;
  color: var(--ink);
}

.circle-name:hover {
  color: var(--green-700);
  text-decoration: underline;
}

.circle-list li.owner {
  border-color: var(--green-400);
  background: var(--green-50);
}

.owner-badge {
  display: inline-block;
  margin-left: 0.5rem;
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  background: var(--green-100);
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--green-700);
}

button {
  background: var(--white);
  color: var(--green-700);
  border: 1px solid var(--green-400);
}

button:hover {
  background: var(--green-100);
  color: var(--green-700);
}
</style>
