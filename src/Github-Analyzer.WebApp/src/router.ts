import type { Pinia } from 'pinia'
import {
  createRouter,
  createWebHistory,
  type RouteLocationNormalized,
  type RouteRecordRaw,
} from 'vue-router'
import AuthCallbackPage from './pages/AuthCallbackPage.vue'
import DashboardPage from './pages/DashboardPage.vue'
import LoginPage from './pages/LoginPage.vue'
import RegisterPage from './pages/RegisterPage.vue'
import { useAuthStore } from './stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    guestOnly?: boolean
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/analysis/new',
  },
  {
    path: '/analysis/new',
    name: 'analysis-new',
    component: DashboardPage,
    meta: { requiresAuth: true },
  },
  {
    path: '/analysis/:jobId',
    name: 'analysis-job',
    component: DashboardPage,
    meta: { requiresAuth: true },
  },
  {
    path: '/login',
    name: 'login',
    component: LoginPage,
    meta: { guestOnly: true },
  },
  {
    path: '/register',
    name: 'register',
    component: RegisterPage,
    meta: { guestOnly: true },
  },
  {
    path: '/auth/callback',
    name: 'auth-callback',
    component: AuthCallbackPage,
  },
]

function needsAuth(route: RouteLocationNormalized) {
  return route.matched.some(record => record.meta.requiresAuth)
}

function guestOnly(route: RouteLocationNormalized) {
  return route.matched.some(record => record.meta.guestOnly)
}

export function createAppRouter(pinia: Pinia) {
  const router = createRouter({
    history: createWebHistory(),
    routes,
  })

  router.beforeEach(async to => {
    const authStore = useAuthStore(pinia)
    await authStore.initialize()

    if (needsAuth(to) && !authStore.isAuthenticated) {
      return { name: 'login' }
    }

    if (guestOnly(to) && authStore.isAuthenticated) {
      return { name: 'analysis-new' }
    }

    return true
  })

  return router
}
