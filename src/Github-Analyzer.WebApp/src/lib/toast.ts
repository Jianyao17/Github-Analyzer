class ToastManager 
{
  private listeners: ((message: string, type: 'error' | 'success') => void)[] = [];

  subscribe(listener: (message: string, type: 'error' | 'success') => void) 
  {
    this.listeners.push(listener);
    return () => 
    {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  showError(message: string) 
  {
    this.listeners.forEach((listener) => listener(message, 'error'));
  }

  showSuccess(message: string) 
  {
    this.listeners.forEach((listener) => listener(message, 'success'));
  }
}

export default new ToastManager();
