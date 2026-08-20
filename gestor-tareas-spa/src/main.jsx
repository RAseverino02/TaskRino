import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { App } from './App'
import { ErrorBoundary } from './components/ErrorBoundary'
import './styles.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <ErrorBoundary><AuthProvider><App /></AuthProvider></ErrorBoundary>
    </BrowserRouter>
  </React.StrictMode>,
)
