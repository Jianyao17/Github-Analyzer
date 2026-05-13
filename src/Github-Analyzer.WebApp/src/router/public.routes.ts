import type { RouteRecordRaw } from "vue-router";

export const publicRoutes: RouteRecordRaw = 
{
  path: "/",
  children: [
    {
      path: "",
      name: "public.home",
      redirect: { name: 'public.login' }
    },
    {
      path: "login",
      name: "public.login",
      component: () => import("../pages/public/LoginPage.vue"),
      meta: { guestOnly: true }
    },
    {
      path: "register",
      name: "public.register",
      component: () => import("../pages/public/RegisterPage.vue"),
      meta: { guestOnly: true }
    },
    {
      path: "auth/callback",
      name: "public.auth-callback",
      component: () => import("../pages/public/AuthCallbackPage.vue"),
    },
    {
      path: ":pathMatch(.*)*",
      name: "public.not-found",
      component: () => import("../pages/public/NotFoundPage.vue"),
    }
  ]
}
