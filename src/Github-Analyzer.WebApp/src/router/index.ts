import { createRouter, createWebHistory } from 'vue-router';
import { publicRoutes } from './public.routes';
import { mainRoutes } from './main.routes';
import { useAuthStore } from '../stores/auth.store';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  scrollBehavior: () => ({ top: 0 }),
  routes: [
    publicRoutes,
    mainRoutes
  ],
});

router.beforeEach(async (to) => 
{
  const auth = useAuthStore();
  
  // Initialize auth state if not already done
  if (!auth.initialized) 
  {
    await auth.initialize();
  }

  const requiresAuth = to.matched.some(record => record.meta.requiresAuth);
  const guestOnly = to.matched.some(record => record.meta.guestOnly);

  if (requiresAuth && !auth.isAuthenticated) 
  {
    return { name: 'public.login', query: { redirect: to.fullPath } };
  }

  if (guestOnly && auth.isAuthenticated) 
  {
    return { name: 'app.dashboard' };
  }

  return true;
});

export default router;
