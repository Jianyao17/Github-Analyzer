import type { RouteRecordRaw } from 'vue-router';

export const publicRoutes: RouteRecordRaw = 
{
  path: '/',
  children: [
    {
      path: '',
      name: 'public.home',
      redirect: { name: 'public.login' }
    },
    {
      path: 'login',
      name: 'public.login',
      component: () => import('../pages/public/LoginPage.vue'),
      meta: { guestOnly: true }
    },
    {
      path: 'register',
      name: 'public.register',
      component: () => import('../pages/public/RegisterPage.vue'),
      meta: { guestOnly: true }
    },
    {
      path: 'auth/callback',
      name: 'public.auth-callback',
      component: () => import('../pages/public/AuthCallbackPage.vue'),
    },
    {
      path: 'auth/verify-email',
      name: 'public.verify-email',
      component: () => import('../pages/public/VerifyEmailPage.vue'),
      meta: { guestOnly: true }
    },
    {
      path: 'auth/forgot-password',
      name: 'public.forgot-password',
      component: () => import('../pages/public/ForgotPasswordPage.vue'),
      meta: { guestOnly: true }
    },
    {
      path: 'auth/reset-password',
      name: 'public.reset-password',
      component: () => import('../pages/public/ResetPasswordPage.vue'),
      meta: { guestOnly: true }
    },
    {
      path: ':pathMatch(.*)*',
      name: 'public.not-found',
      component: () => import('../pages/public/NotFoundPage.vue'),
    }
  ]
};
