import type { GraphEvents } from '@graph/types/events';

type Listener<T> = (payload: T) => void;
type Unsubscribe  = () => void;

/**
 * A strictly-typed event bus used for decoupling communication between the engine,
 * rendering passes, layout algorithms, and plugins.
 */
export class EventBus 
{
  private _listeners = new Map<string, Set<Listener<unknown>>>();

  /**
   * Subscribes a listener to a specific event.
   * 
   * @param event The event key defined in GraphEvents.
   * @param listener The callback function that receives the event payload.
   * @returns A function to unsubscribe this listener.
   */
  on<K extends keyof GraphEvents>(
    event:    K,
    listener: Listener<GraphEvents[K]>,
  ): Unsubscribe 
  {
    let set = this._listeners.get(event as string);
    if (!set) 
    {
      set = new Set();
      this._listeners.set(event as string, set);
    }
    
    set.add(listener as Listener<unknown>);
    return () => set!.delete(listener as Listener<unknown>);
  }

  /**
   * Emits an event with the corresponding payload to all registered listeners.
   * 
   * @param event The event key to emit.
   * @param payload The data to pass to the listeners.
   */
  emit<K extends keyof GraphEvents>(event: K, payload: GraphEvents[K]): void 
  {
    this._listeners
      .get(event as string)
      ?.forEach(fn => fn(payload));
  }

  /**
   * Removes all registered event listeners. Useful during teardown to prevent leaks.
   */
  clear(): void 
  {
    this._listeners.clear();
  }
}
