import type { RouteRecordRaw } from 'vue-router'
import DashboardPage from './views/DashboardPage.vue'
import LoginPage from './views/LoginPage.vue'

export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'dashboard',
    component: DashboardPage,
  },
  {
    path: '/login',
    name: 'login',
    component: LoginPage,
  },
  {
    path: '/auth/callback',
    name: 'auth-callback',
    component: DashboardPage,
  },
]
