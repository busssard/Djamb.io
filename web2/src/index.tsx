/// <reference types="vite-plugin-pwa/client" />
import React from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { Provider } from 'react-redux';
import App from './components/App/App';
import { store } from './redux';
import { registerSW } from 'virtual:pwa-register';

const container = document.getElementById('root');
const root = createRoot(container!);
root.render(
  <React.StrictMode>
    <Provider store={store}>
      <App />
    </Provider>
  </React.StrictMode>,
);

const updateSW = registerSW({
  onNeedRefresh() {
    if (confirm('New version available. Reload?')) {
      updateSW(true);
    }
  },
  onOfflineReady() {
    console.log('App ready for offline use');
  },
});
