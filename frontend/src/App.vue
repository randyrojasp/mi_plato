<script setup>
import { computed, onMounted, ref } from 'vue'

const API_URL = import.meta.env.VITE_API_URL || ''

const adminName = ref('Clase de nutricion')
const roomCodeInput = ref('')
const playerName = ref('')
const room = ref(null)
const player = ref(null)
const products = ref([])
const ranking = ref([])
const cart = ref([])
const result = ref(null)
const loading = ref(false)
const error = ref('')
const selectedCategory = ref('Todos')

const categories = ['Todos', 'Verdura', 'Fruta', 'Cereal', 'Proteina', 'Procesado', 'Agua']

const money = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  maximumFractionDigits: 0,
})

const currentBudget = computed(() => room.value?.initialBudgetColones ?? player.value?.currentBudgetColones ?? 0)
const spent = computed(() => cart.value.reduce((total, item) => total + item.product.priceColones * item.quantity, 0))
const remaining = computed(() => currentBudget.value - spent.value)
const canSubmit = computed(() => player.value && cart.value.length > 0 && remaining.value >= 0)

const filteredProducts = computed(() => {
  if (selectedCategory.value === 'Todos') return products.value
  return products.value.filter((product) => product.category === selectedCategory.value)
})

const plateGroups = computed(() => {
  const groups = cart.value.reduce((acc, item) => {
    acc[item.product.category] ||= []
    acc[item.product.category].push(item)
    return acc
  }, {})

  return categories.filter((category) => category !== 'Todos').map((category) => ({
    category,
    items: groups[category] || [],
  }))
})

function rankingTag(entry) {
  if (!entry.rounds) {
    return { label: 'Sin plato', class: 'bg-[#f5f0e6] text-[#8b7b68]' }
  }

  if (entry.lastScore >= 85) {
    return { label: 'Plato estrella', class: 'bg-[#314b33] text-white' }
  }

  if (entry.lastScore >= 70) {
    return { label: 'Muy saludable', class: 'bg-[#dff1df] text-[#314b33]' }
  }

  if (entry.lastScore >= 55) {
    return { label: 'Buen intento', class: 'bg-[#fff1cf] text-[#7a4d10]' }
  }

  if (entry.lastScore >= 40) {
    return { label: 'Puede mejorar', class: 'bg-[#ffe1cf] text-[#8c3d2d]' }
  }

  return { label: 'En progreso', class: 'bg-red-50 text-red-700' }
}

async function api(path, options = {}) {
  const response = await fetch(`${API_URL}${path}`, {
    headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
    ...options,
  })

  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw new Error(body.message || 'No se pudo completar la accion.')
  }

  if (response.status === 204) return null
  return response.json()
}

async function loadProducts() {
  products.value = await api('/api/products')
}

async function createRoom() {
  await run(async () => {
    room.value = await api('/api/rooms', {
      method: 'POST',
      body: JSON.stringify({ name: adminName.value, initialBudgetColones: 5000 }),
    })
    roomCodeInput.value = room.value.code
    localStorage.setItem('plato-room', JSON.stringify(room.value))
    await refreshRanking()
  })
}

async function joinRoom() {
  await run(async () => {
    const existingRoom = await api(`/api/rooms/${roomCodeInput.value}`)
    const newPlayer = await api('/api/players', {
      method: 'POST',
      body: JSON.stringify({ name: playerName.value, roomCode: existingRoom.code }),
    })

    room.value = existingRoom
    player.value = newPlayer
    localStorage.setItem('plato-room', JSON.stringify(existingRoom))
    localStorage.setItem('plato-player', JSON.stringify(newPlayer))
    await refreshRanking()
  })
}

function addProduct(product) {
  result.value = null
  const existing = cart.value.find((item) => item.product.id === product.id)
  if (existing) {
    existing.quantity += 1
  } else {
    cart.value.push({ product, quantity: 1 })
  }
}

function removeProduct(productId) {
  const existing = cart.value.find((item) => item.product.id === productId)
  if (!existing) return
  if (existing.quantity === 1) {
    cart.value = cart.value.filter((item) => item.product.id !== productId)
  } else {
    existing.quantity -= 1
  }
}

async function submitPlate() {
  await run(async () => {
    result.value = await api('/api/games/submit', {
      method: 'POST',
      body: JSON.stringify({
        playerId: player.value.id,
        items: cart.value.map((item) => ({ productId: item.product.id, quantity: item.quantity })),
      }),
    })

    player.value = {
      ...player.value,
      coinsColones: player.value.coinsColones + result.value.coinsEarnedColones,
      currentBudgetColones: result.value.newBudgetColones,
    }
    cart.value = []
    localStorage.setItem('plato-player', JSON.stringify(player.value))
    await refreshRanking()
  })
}

async function refreshRanking() {
  if (!room.value?.code) return
  const roomCode = encodeURIComponent(room.value.code)
  ranking.value = await api(`/api/rooms/${roomCode}/ranking?t=${Date.now()}`)
}

async function updateRanking() {
  await run(refreshRanking)
}

function leaveGame() {
  player.value = null
  result.value = null
  cart.value = []
  localStorage.removeItem('plato-player')
}

async function run(action) {
  loading.value = true
  error.value = ''
  try {
    await action()
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  const savedRoom = localStorage.getItem('plato-room')
  const savedPlayer = localStorage.getItem('plato-player')
  if (savedRoom) room.value = JSON.parse(savedRoom)
  if (savedPlayer) player.value = JSON.parse(savedPlayer)

  await run(async () => {
    await loadProducts()
    await refreshRanking()
  })
})
</script>

<template>
  <main class="min-h-screen bg-[#f5f0e6] px-3 py-3 sm:px-6 sm:py-5">
    <section class="mx-auto grid max-w-6xl gap-5 lg:grid-cols-[1.05fr_0.95fr]">
      <div class="min-w-0 rounded-[1.5rem] bg-[#314b33] p-4 text-white shadow-2xl shadow-black/20 sm:rounded-[2rem] sm:p-8">
        <p class="text-xs font-bold uppercase tracking-[0.22em] text-[#f6c65b] sm:text-sm sm:tracking-[0.28em]">Juego de clase</p>
        <h1 class="mt-3 text-4xl font-black leading-none sm:text-6xl">Mi Plato Tico</h1>
        <p class="mt-4 max-w-2xl text-base text-white/80 sm:text-lg">
          Compra alimentos con presupuesto en colones, arma tu plato y gana monedas segun salud, balance y nivel de procesados.
        </p>

        <div class="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div class="rounded-2xl bg-white/10 p-3 sm:p-4">
            <p class="text-xs uppercase text-white/60">Sala</p>
            <p class="text-xl font-black sm:text-2xl">{{ room?.code || 'Nueva' }}</p>
          </div>
          <div class="rounded-2xl bg-white/10 p-3 sm:p-4">
            <p class="text-xs uppercase text-white/60">Presupuesto</p>
            <p class="text-xl font-black sm:text-2xl">{{ money.format(currentBudget) }}</p>
          </div>
          <div class="col-span-2 rounded-2xl bg-white/10 p-3 sm:col-span-1 sm:p-4">
            <p class="text-xs uppercase text-white/60">Jugador</p>
            <p class="truncate text-xl font-black sm:text-2xl">{{ player?.name || 'Sin entrar' }}</p>
          </div>
        </div>
      </div>

      <div class="min-w-0 rounded-[1.5rem] border border-[#dfd2bd] bg-white p-4 shadow-xl shadow-black/10 sm:rounded-[2rem] sm:p-6">
        <h2 class="text-2xl font-black text-[#314b33]">Entrada a la actividad</h2>
        <p class="mt-1 text-sm text-[#6d6255]">Crea una sala para proyectar el codigo o entra con el codigo del profesor.</p>

        <div class="mt-5 grid gap-3">
          <input v-model="adminName" class="rounded-2xl border border-[#dfd2bd] px-4 py-3 outline-none focus:border-[#f1a33b]" placeholder="Nombre de la clase" />
          <button class="rounded-2xl bg-[#f1a33b] px-4 py-3 font-black text-[#2f261d] disabled:opacity-50" :disabled="loading" @click="createRoom">
            Crear sala
          </button>
        </div>

        <div class="my-5 h-px bg-[#eee3d0]"></div>

        <div class="grid gap-3 sm:grid-cols-2">
          <input v-model="roomCodeInput" class="rounded-2xl border border-[#dfd2bd] px-4 py-3 uppercase outline-none focus:border-[#f1a33b]" placeholder="Codigo de sala" />
          <input v-model="playerName" class="rounded-2xl border border-[#dfd2bd] px-4 py-3 outline-none focus:border-[#f1a33b]" placeholder="Tu nombre" />
          <button class="rounded-2xl bg-[#314b33] px-4 py-3 font-black text-white disabled:opacity-50 sm:col-span-2" :disabled="loading || !roomCodeInput || !playerName" @click="joinRoom">
            Entrar como estudiante
          </button>
        </div>

        <p v-if="error" class="mt-4 rounded-2xl bg-red-50 p-3 text-sm font-bold text-red-700">{{ error }}</p>
      </div>
    </section>

    <section v-if="player" class="mx-auto mt-4 grid max-w-6xl min-w-0 gap-4 sm:mt-5 sm:gap-5 lg:grid-cols-[0.95fr_1.05fr]">
      <div class="min-w-0 overflow-hidden rounded-[1.5rem] bg-white p-4 shadow-xl shadow-black/10 sm:rounded-[2rem] sm:p-6">
        <div class="grid gap-2 sm:flex sm:items-center sm:justify-between sm:gap-3">
          <div>
            <h2 class="text-2xl font-black text-[#314b33]">Mercado</h2>
            <p class="text-sm text-[#6d6255]">Elige lo que pondras en el plato.</p>
          </div>
          <button class="justify-self-start rounded-full bg-[#fff1e7] px-3 py-2 text-sm font-bold text-[#8c3d2d] sm:justify-self-auto sm:bg-transparent sm:px-0 sm:py-0" @click="leaveGame">Salir</button>
        </div>

        <div class="-mx-4 mt-4 flex gap-2 overflow-x-auto px-4 pb-2 sm:mx-0 sm:px-0">
          <button
            v-for="category in categories"
            :key="category"
            class="whitespace-nowrap rounded-full px-4 py-2 text-sm font-black"
            :class="selectedCategory === category ? 'bg-[#314b33] text-white' : 'bg-[#f5f0e6] text-[#6d6255]'"
            @click="selectedCategory = category"
          >
            {{ category }}
          </button>
        </div>

        <div class="mt-4 grid gap-2 sm:grid-cols-2 sm:gap-3 lg:grid-cols-1">
          <button
            v-for="product in filteredProducts"
            :key="product.id"
            class="grid grid-cols-[2.75rem_1fr] gap-3 rounded-2xl border border-[#eee3d0] bg-[#fffaf1] p-3 text-left transition active:scale-[0.99] sm:grid-cols-[3.5rem_1fr] sm:rounded-3xl lg:grid-cols-[3.5rem_1fr_auto]"
            @click="addProduct(product)"
          >
            <span class="grid h-11 w-11 place-items-center rounded-2xl bg-white font-black text-[#314b33] shadow-inner sm:h-14 sm:w-14">{{ product.emoji }}</span>
            <span class="min-w-0">
              <span class="block truncate font-black text-[#2f261d] sm:whitespace-normal">{{ product.name }}</span>
              <span class="mt-1 flex flex-wrap gap-1 text-[0.68rem] font-bold uppercase leading-snug text-[#8b7b68] sm:text-xs">
                <span class="rounded-full bg-white px-2 py-0.5">{{ product.kind }}</span>
                <span class="rounded-full bg-white px-2 py-0.5">Salud {{ product.health }}</span>
                <span class="rounded-full bg-white px-2 py-0.5">Quimicos {{ product.chemicals }}</span>
              </span>
              <span class="mt-2 block font-black text-[#314b33] lg:hidden">{{ money.format(product.priceColones) }}</span>
            </span>
            <span class="hidden self-center text-right font-black text-[#314b33] lg:block">{{ money.format(product.priceColones) }}</span>
          </button>
        </div>
      </div>

      <div class="grid min-w-0 gap-5">
        <div class="min-w-0 rounded-[1.5rem] bg-white p-4 shadow-xl shadow-black/10 sm:rounded-[2rem] sm:p-6">
          <div class="grid gap-3 sm:flex sm:items-start sm:justify-between">
            <div>
              <h2 class="text-2xl font-black text-[#314b33]">Tu plato</h2>
              <p class="text-sm text-[#6d6255]">Gastado {{ money.format(spent) }} / queda {{ money.format(remaining) }}</p>
            </div>
            <button class="w-full rounded-2xl bg-[#f1a33b] px-4 py-3 font-black text-[#2f261d] disabled:opacity-40 sm:w-auto" :disabled="loading || !canSubmit" @click="submitPlate">
              Evaluar
            </button>
          </div>

          <div class="mt-5 grid gap-2 sm:grid-cols-2 sm:gap-3">
            <div v-for="group in plateGroups" :key="group.category" class="rounded-2xl border-2 border-dashed border-[#dfd2bd] bg-[#fffaf1] p-3 sm:min-h-28 sm:rounded-3xl sm:p-4">
              <div class="flex items-center justify-between gap-3">
                <p class="font-black text-[#314b33]">{{ group.category }}</p>
                <span class="rounded-full bg-white px-2 py-1 text-xs font-black text-[#8b7b68]">{{ group.items.length }}</span>
              </div>
              <div v-if="group.items.length" class="mt-3 grid gap-2">
                <button v-for="item in group.items" :key="item.product.id" class="grid grid-cols-[auto_1fr_auto] items-center gap-2 rounded-2xl bg-white px-3 py-2 text-left text-sm font-bold shadow-sm" @click="removeProduct(item.product.id)">
                  <span>{{ item.product.emoji }}</span>
                  <span class="truncate">{{ item.product.name }}</span>
                  <span class="text-[#314b33]">{{ item.quantity }}x</span>
                </button>
              </div>
              <p v-else class="mt-2 text-sm text-[#8b7b68]">Vacio</p>
            </div>
          </div>

          <p v-if="remaining < 0" class="mt-4 rounded-2xl bg-red-50 p-3 text-sm font-bold text-red-700">Te pasaste del presupuesto.</p>
        </div>

        <div v-if="result" class="rounded-[1.5rem] bg-[#314b33] p-4 text-white shadow-xl shadow-black/10 sm:rounded-[2rem] sm:p-6">
          <p class="text-sm font-bold uppercase tracking-[0.24em] text-[#f6c65b]">Resultado ronda {{ result.round }}</p>
          <div class="mt-3 flex items-end justify-between gap-4">
            <h2 class="text-5xl font-black">{{ result.score }}/100</h2>
            <p class="text-right text-xl font-black">+{{ money.format(result.coinsEarnedColones) }}</p>
          </div>
          <p class="mt-3 text-white/85">{{ result.message }}</p>
          <div class="mt-4 grid grid-cols-3 gap-2 text-center text-xs sm:text-sm">
            <span class="rounded-2xl bg-white/10 p-3">Salud<br><b>{{ result.healthTotal }}</b></span>
            <span class="rounded-2xl bg-white/10 p-3">Quimicos<br><b>{{ result.chemicalTotal }}</b></span>
            <span class="rounded-2xl bg-white/10 p-3">Balance<br><b>{{ result.balanceScore }}</b></span>
          </div>
        </div>
      </div>
    </section>

    <section v-if="room" class="mx-auto mt-4 max-w-6xl rounded-[1.5rem] bg-white p-4 shadow-xl shadow-black/10 sm:mt-5 sm:rounded-[2rem] sm:p-6">
      <div class="grid gap-3 sm:flex sm:items-center sm:justify-between">
        <div>
          <h2 class="text-2xl font-black text-[#314b33]">Ranking</h2>
          <p class="text-sm text-[#6d6255]">Sala {{ room.code }}. Se ordena por el ultimo puntaje.</p>
        </div>
        <button class="rounded-2xl bg-[#f5f0e6] px-4 py-3 font-black text-[#314b33] disabled:opacity-50 sm:py-2" :disabled="loading" @click="updateRanking">
          {{ loading ? 'Actualizando...' : 'Actualizar' }}
        </button>
      </div>

      <div class="mt-4 grid gap-2">
        <div v-for="(entry, index) in ranking" :key="entry.playerId" class="grid grid-cols-[2rem_1fr] items-center gap-3 rounded-2xl bg-[#fffaf1] p-3 sm:grid-cols-[2rem_1fr_auto]">
          <span class="font-black text-[#f1a33b]">{{ index + 1 }}</span>
          <span class="min-w-0">
            <span class="flex flex-wrap items-center gap-2">
              <span class="font-black">{{ entry.name }}</span>
              <span class="rounded-full px-2 py-1 text-[0.68rem] font-black uppercase leading-none" :class="rankingTag(entry).class">
                {{ rankingTag(entry).label }}
              </span>
            </span>
            <span class="block text-xs text-[#8b7b68]">{{ entry.rounds }} rondas / {{ entry.lastMessage }}</span>
          </span>
          <span class="col-span-2 text-right font-black sm:col-span-1">{{ entry.lastScore }} pts<br><small>{{ money.format(entry.coinsColones) }}</small></span>
        </div>
        <p v-if="!ranking.length" class="rounded-2xl bg-[#fffaf1] p-4 text-center text-sm text-[#8b7b68]">Aun no hay jugadores.</p>
      </div>
    </section>
  </main>
</template>
