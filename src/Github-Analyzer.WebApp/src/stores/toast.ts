import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export type ToastTone = 'success' | 'error' | 'info'

export interface ToastItem {
  id: string
  title: string
  description?: string
  tone: ToastTone
  createdAt: number
  durationMs: number
}

const defaultDurationMs = 3500

function createId() {
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export const useToastStore = defineStore('toast', () => {
  const items = ref<ToastItem[]>([])
  const hasToasts = computed(() => items.value.length > 0)

  function add(payload: {
    title: string
    description?: string
    tone?: ToastTone
    durationMs?: number
  }) {
    const toast: ToastItem = {
      id: createId(),
      title: payload.title,
      description: payload.description,
      tone: payload.tone ?? 'info',
      createdAt: Date.now(),
      durationMs: payload.durationMs ?? defaultDurationMs,
    }

    items.value = [toast, ...items.value].slice(0, 4)

    window.setTimeout(() => {
      remove(toast.id)
    }, toast.durationMs)
  }

  function remove(id: string) {
    items.value = items.value.filter(item => item.id !== id)
  }

  function clear() {
    items.value = []
  }

  return {
    add,
    clear,
    hasToasts,
    items,
    remove,
  }
})
