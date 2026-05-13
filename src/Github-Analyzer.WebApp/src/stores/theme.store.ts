import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

type Theme = 'light' | 'dark';

export const useThemeStore = defineStore('theme', () => 
{
  const currentTheme = ref<Theme>('light');
  const theme = computed(() => currentTheme.value);

  const initTheme = () => 
  {
    const storedTheme = localStorage.getItem('theme') as Theme | null;
    if (storedTheme)
    { 
      currentTheme.value = storedTheme; 
    }
    else 
    {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      currentTheme.value = prefersDark ? 'dark' : 'light';
    }

    setTheme(currentTheme.value);
  };

  const toggleTheme = () => 
  {
    currentTheme.value = currentTheme.value === 'light' ? 'dark' : 'light';
    setTheme(currentTheme.value);
  };

  const setTheme = (theme: Theme) => 
  {
    const html = document.documentElement;
    html.classList.remove('dark');
    
    if (theme === 'dark')
    { 
      html.classList.add('dark'); 
    }

    localStorage.setItem('theme', theme);
  };

  return {
    theme,
    initTheme,
    toggleTheme,
    setTheme,
  };
});
