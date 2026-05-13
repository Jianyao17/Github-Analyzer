import { ref, computed, watch } from 'vue'
import { useBreakpoints, breakpointsTailwind, useStorage } from '@vueuse/core'

export function useSidebar() 
{
  const breakpoints = useBreakpoints(breakpointsTailwind)

  const isMobile = breakpoints.smaller('md')
  const isDesktopSmall = breakpoints.between('md', 'lg')
  const persistedCollapsed = useStorage<boolean>('sidebar:collapsed', false)

  const isCollapsed = ref(false)
  const isOpen = ref(true)

  watch(isDesktopSmall, (val) => 
  {
    if (val) 
    {
      isCollapsed.value = true
    }
    else 
    {
      isCollapsed.value = persistedCollapsed.value
    }
  }, { immediate: true })

  watch(isMobile, (val) => 
  {
    if (val) 
    {
      isOpen.value = false
      isCollapsed.value = false
    } 
    else 
    {
      isCollapsed.value = persistedCollapsed.value
    }
  }, { immediate: true })

  const setCollapsed = (val: boolean) =>
  {
    isCollapsed.value = val
    if (isMobile.value === false)
    {
      persistedCollapsed.value = val
    }
  }

  const sidebarWidth = computed(() => 
  {
    if (isMobile.value) return 0
    return isCollapsed.value ? 80 : 260
  })

  return {
    isMobile,
    isCollapsed,
    isOpen,
    sidebarWidth,
    toggleCollapse: () => setCollapsed(!isCollapsed.value),
    open: () => (isOpen.value = true),
    close: () => (isOpen.value = false),
  }
}
