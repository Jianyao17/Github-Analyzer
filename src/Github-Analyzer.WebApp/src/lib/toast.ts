import type { ToasterProps, ToastProps } from '@nuxt/ui';

type ToastType = NonNullable<ToastProps['color']>;

type ToastMeta = {
  defaultTitle: string
  icon: string
  color: ToastType
};

type ToastApi = 
{
  add: (options: Partial<ToastProps>) => ToastProps
};

const toastMeta: Record<ToastType, ToastMeta> = 
{
  primary:   { defaultTitle: 'Notice',  icon: 'i-lucide-bell',            color: 'primary'    },
  secondary: { defaultTitle: 'Notice',  icon: 'i-lucide-bell',            color: 'secondary'  },
  success:   { defaultTitle: 'Success', icon: 'i-lucide-check-circle',    color: 'success'    },
  info:      { defaultTitle: 'Info',    icon: 'i-lucide-info',            color: 'info'       },
  warning:   { defaultTitle: 'Warning', icon: 'i-lucide-triangle-alert',  color: 'warning'    },
  error:     { defaultTitle: 'Error',   icon: 'i-lucide-alert-circle',    color: 'error'      },
  neutral:   { defaultTitle: 'Notice',  icon: 'i-lucide-bell',            color: 'neutral'    }
};

// Global toaster defaults used by NApp.
export const toaster: ToasterProps = 
{
  position: 'top-center',
  duration: 3000,
  max: 3,
};

// Bridge to the Nuxt UI toast API (set once in App.vue).
let toastApi: ToastApi | null = null;

// Call once after useToast() is available.
export function setToastApi(api: ToastApi) 
{
  toastApi = api;
}

type ToastOptions = Partial<ToastProps> & 
{
  message?: string
  toastType?: ToastType
};

// Main helper that merges defaults with user-provided options.
export function showToast(options: ToastOptions = {}) 
{
  if (!toastApi) 
  {
    console.warn('Toast API is not ready yet.');
    return;
  }

  // Determine type and related defaults
  const resolvedType = options.color ?? options.toastType ?? 'neutral';
  const resolvedDescription = options.description ?? options.message;
  const meta = toastMeta[resolvedType];

  toastApi.add({
    ...options,
    orientation: options.orientation ?? 'horizontal',
    title: options.title ?? meta.defaultTitle,
    description: resolvedDescription,
    color: options.color ?? meta.color,
    icon: options.icon ?? meta.icon,
  });
}

export function showError(options: ToastOptions = {}) 
{
  showToast({ toastType: 'error', ...options });
}

export function showSuccess(options: ToastOptions = {}) 
{
  showToast({ toastType: 'success', ...options });
}

export function showInfo(options: ToastOptions = {}) 
{
  showToast({ toastType: 'info', ...options });
}
