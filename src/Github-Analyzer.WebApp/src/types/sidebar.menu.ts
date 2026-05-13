export interface SidebarMenuItem {
  label: string
  icon?: string
  to?: string
  children?: SidebarMenuItem[]
  disabled?: boolean
}

export const sidebarMenu: SidebarMenuItem[] = 
[
  {
    label: 'Dashboard',
    icon: 'lucide:layout-dashboard',
    to: '/app/dashboard',
  },
]
