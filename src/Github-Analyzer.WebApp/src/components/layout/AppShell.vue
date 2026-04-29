<template>
  <UApp>
    <div class="min-h-screen bg-(--ui-bg)">
      <div class="flex min-h-screen">
        <aside class="w-72 shrink-0 border-r border-(--ui-border-muted) bg-(--ui-bg-elevated)">
          <slot name="sidebar" />
        </aside>
        <main class="min-w-0 flex-1">
          <div class="min-h-screen p-6 lg:p-8">
            <slot />
          </div>
        </main>
      </div>
    </div>
  </UApp>
</template>
