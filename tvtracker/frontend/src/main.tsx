import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'
import App from './App'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 1000 * 60 * 5 } }
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
      <Toaster position="top-right" toastOptions={{
        style: { background: '#1a1a2e', color: '#e2e8f0', border: '1px solid #2d2d44' },
        success: { iconTheme: { primary: '#22c55e', secondary: '#1a1a2e' } },
        error: { iconTheme: { primary: '#ef4444', secondary: '#1a1a2e' } },
      }} />
    </QueryClientProvider>
  </React.StrictMode>,
)
