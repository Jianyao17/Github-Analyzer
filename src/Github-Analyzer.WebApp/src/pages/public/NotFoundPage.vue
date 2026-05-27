<template>
  <div class="
    relative min-h-screen overflow-hidden bg-default
    dark:bg-default
  "
  >
    <div class="
      pointer-events-none absolute inset-0 bg-linear-to-br from-primary/10
      via-transparent to-secondary/10
    "
    />
    <div class="
      pointer-events-none absolute top-16 -left-20 h-64 w-64 rounded-full
      bg-primary/20 blur-3xl
    "
    />
    <div class="
      pointer-events-none absolute -right-24 bottom-10 h-72 w-72 rounded-full
      bg-secondary/20 blur-3xl
    "
    />

    <div class="relative z-10 flex min-h-screen items-center">
      <div class="mx-auto w-full max-w-3xl px-6 py-16 text-center">
        <div class="space-y-6">
          <div class="
            text-[96px] leading-none font-black text-highlighted
            sm:text-[140px]
          "
          >
            404
          </div>
          <div class="space-y-3">
            <h1 class="
              text-3xl font-semibold text-highlighted
              sm:text-4xl
            "
            >
              Page not found
            </h1>
            <p class="mx-auto max-w-lg text-base text-muted">
              The page you are looking for does not exist. Check the address or
              return to the home page.
            </p>
          </div>
          <div class="flex flex-wrap justify-center gap-3">
            <NButton to="/"
              color="primary"
            >
              Back to Home
            </NButton>
            <NButton to="/login"
              color="neutral"
              variant="outline"
            >
              Go to Login
            </NButton>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
