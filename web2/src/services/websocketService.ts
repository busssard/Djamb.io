import { store } from '../redux';
import { gameUpdated } from '../redux/activeGame/actionFactory';
import { StateAndEventResponseDtoFromJSON } from '../api-client';
import { playGong } from './soundService';

class WebSocketService {
  private socket: WebSocket | null = null;
  private apiUrl: string = '';
  private reconnectDelay = 1000;
  private maxReconnectDelay = 30000;
  private shouldReconnect = false;

  connect(apiUrl: string): void {
    this.apiUrl = apiUrl;
    this.shouldReconnect = true;
    this.reconnectDelay = 1000;
    this.doConnect();
  }

  disconnect(): void {
    this.shouldReconnect = false;
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }
  }

  private doConnect(): void {
    if (!this.apiUrl || !this.shouldReconnect) return;

    const wsUrl = `${this.apiUrl}/api/notifications/ws`.replace('http:', 'ws:').replace('https:', 'wss:');

    try {
      const socket = new WebSocket(wsUrl);

      socket.onopen = () => {
        console.log('[WS] Connected to', wsUrl);
        this.reconnectDelay = 1000;
      };

      socket.onmessage = (e: MessageEvent) => {
        try {
          const json = JSON.parse(e.data as string);
          console.log('[WS] Received message:', json);
          const response = StateAndEventResponseDtoFromJSON(json);
          console.log('[WS] Parsed response, game id:', response.game?.id);

          // Update active game in Redux if it matches
          const state = store.getState();
          if (state.activeGame.game && response.game.id === state.activeGame.game.id) {
            console.log('[WS] Dispatching gameUpdated');
            store.dispatch(gameUpdated(response));
          } else {
            console.log('[WS] No active game match, active:', state.activeGame.game?.id);
          }

          // Check if it's now the current user's turn in any game
          const session = state.session;
          if (session.user && response.game.turnCycle && response.game.turnCycle.length > 0) {
            const currentPlayerId = response.game.turnCycle[0];
            const currentPlayer = response.game.players?.find((p) => p.id === currentPlayerId);
            if (currentPlayer && currentPlayer.userId === session.user.id) {
              playGong();
            }
          }
        } catch (err) {
          console.error('[WS] Error processing message:', err);
        }
      };

      socket.onclose = () => {
        this.socket = null;
        if (this.shouldReconnect) {
          setTimeout(() => this.doConnect(), this.reconnectDelay);
          this.reconnectDelay = Math.min(this.reconnectDelay * 2, this.maxReconnectDelay);
        }
      };

      socket.onerror = () => {
        // onclose will fire after onerror, handling reconnect
      };

      this.socket = socket;
    } catch {
      // Connection failed, try again
      if (this.shouldReconnect) {
        setTimeout(() => this.doConnect(), this.reconnectDelay);
        this.reconnectDelay = Math.min(this.reconnectDelay * 2, this.maxReconnectDelay);
      }
    }
  }
}

export const wsService = new WebSocketService();
