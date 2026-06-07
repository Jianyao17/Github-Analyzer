import type { GraphData }     from '@graph/types/graph-data';
import type { GraphPlugin }   from '@graph/types/plugin';
import type { GraphContext }  from './GraphContext';

interface RegisteredPlugin {
  plugin:   GraphPlugin;
  priority: number;
}

/**
 * Manages the lifecycle of graph plugins (setup and teardown).
 * Ensures plugins are executed in a deterministic order based on priority.
 */
export class PluginRegistry 
{
  private _plugins: RegisteredPlugin[] = [];

  /**
   * Registers a plugin with a specific execution priority.
   * Lower priority means the plugin sets up earlier and tears down later.
   * Usually, ZoomPlugin uses priority 0 so other plugins can listen to its zoom events.
   * 
   * @param plugin The plugin to register.
   * @param priority Execution priority (default is 5).
   */
  register(plugin: GraphPlugin, priority = 5): this 
  {
    if (this._plugins.some(p => p.plugin.name === plugin.name)) 
    {
      return this; // Prevent duplicate registration
    }
    this._plugins.push({ plugin, priority });
    this._plugins.sort((a, b) => a.priority - b.priority);
    return this;
  }

  /**
   * Initializes all registered plugins by calling their setup methods.
   * Plugins are initialized in ascending order of their priority.
   * 
   * @param ctx The shared GraphContext containing live selections.
   * @param data The new graph data being rendered.
   */
  setupAll(ctx: GraphContext, data: GraphData): void 
  {
    for (const { plugin } of this._plugins) 
    {
      plugin.setup(ctx, data);
    }
  }

  /**
   * Runs the data transformation middleware chain.
   * Modifies ctx.nodes and ctx.edges in-place sequentially.
   */
  transformAll(ctx: GraphContext, data: GraphData): void 
  {
    for (const { plugin } of this._plugins) 
    {
      plugin.transform?.(ctx, data);
    }
  }

  /** 
   * Tears down all plugins to prevent memory leaks or dangling event listeners.
   * Executed in reverse order of initialization (highest priority tears down first).
   */
  teardownAll(): void 
  {
    for (const { plugin } of [...this._plugins].reverse()) 
    {
      try 
      {
        plugin.teardown?.();
      }
      catch (err) 
      {
        console.warn(`[PluginRegistry] teardown error in '${plugin.name}':`, err);
      }
    }
  }

  getPlugin<T extends GraphPlugin>(name: string): T | undefined 
  {
    return this._plugins.find(p => p.plugin.name === name)?.plugin as T | undefined;
  }

  get names(): string[] 
  {
    return this._plugins.map(p => p.plugin.name);
  }
}
