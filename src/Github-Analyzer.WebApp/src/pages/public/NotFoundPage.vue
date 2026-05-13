<template>
  <div class="not-found">
    <h1>404</h1>
    <h2>Page Not Found</h2>
    <p>The page you are looking for doesn't exist or has been moved.</p>
    <RouterLink to="/" class="back-home">Back to Home</RouterLink>
  </div>
</template>

<style scoped>
.not-found {
  height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 20px;
}

h1 { font-size: 80px; font-weight: 800; margin: 0; color: #111827; }
h2 { font-size: 24px; font-weight: 600; margin: 8px 0; color: #374151; }
p { color: #6b7280; margin-bottom: 24px; }

.back-home {
  padding: 10px 20px;
  background-color: #111827;
  color: #ffffff;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 600;
}

.dark h1 { color: #f9fafb; }
.dark h2 { color: #d1d5db; }
.dark .back-home { background-color: #f9fafb; color: #111827; }
</style>
