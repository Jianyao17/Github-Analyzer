import type { RouteRecordRaw } from "vue-router";

export const mainRoutes: RouteRecordRaw = 
{
  path: "/app",
  component: () => import("../components/_Layouts/Dashboard.vue"),
  meta: { requiresAuth: true },
  children: [
    {
      path: "analysis/new",
      name: "app.analysis.new",
      component: () => import("../pages/app/NewAnalysisPage.vue"),
    },
    {
      path: "project/:id",
      name: "app.project-detail",
      component: () => import("../pages/app/ProjectDetailPage.vue"),
    },
    {
      path: ":pathMatch(.*)*",
      name: "app.not-found",
      component: () => import("../pages/public/NotFoundPage.vue"),
    }
  ]
}
