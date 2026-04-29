import { useToastStore } from '../stores/toast'

export function useToastSystem() {
  const toastStore = useToastStore()

  return {
    add: toastStore.add,
    clear: toastStore.clear,
    remove: toastStore.remove,
  }
}
