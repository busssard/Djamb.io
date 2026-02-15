import * as Api from '../utilities/api';
import { gameLoaded, gameUpdated, attemptingGameLoad } from '../redux/activeGame/actionFactory';
import { store } from '../redux';
import { GameParametersDto, CreatePlayerRequestDto } from '../api-client';
import * as Routes from '../utilities/routes';
import { navigateTo } from './navigationController';

export async function blockGameLoading(): Promise<void> {
  const action = attemptingGameLoad();
  store.dispatch(action);
}

export async function loadGame(gameId: number, force?: boolean): Promise<void> {
  if (store.getState().activeGame.isLoadPending && !force) {
    return;
  }

  blockGameLoading();
  const game = await Api.games().apiGamesGameIdGet({ gameId });
  const action = gameLoaded(game);
  store.dispatch(action);
}

export async function createGame(parameters: GameParametersDto): Promise<void> {
  const game = await Api.games().apiGamesPost({ gameParametersDto: parameters });
  const action = gameLoaded(game);
  store.dispatch(action);
  navigateTo(Routes.gameLobby(game.id));
}

export async function addPlayer(gameId: number, request: CreatePlayerRequestDto): Promise<void> {
  const postPlayerRequest = { gameId, createPlayerRequestDto: request };
  const response = await Api.players().apiGamesGameIdPlayersPost(postPlayerRequest);
  const action = gameUpdated(response);
  store.dispatch(action);
}

export async function removePlayer(gameId: number, playerId: number): Promise<void> {
  const deletePlayerRequest = { gameId, playerId };
  const response = await Api.players().apiGamesGameIdPlayersPlayerIdDelete(deletePlayerRequest);
  const action = gameUpdated(response);
  store.dispatch(action);
}

export async function startGame(gameId: number): Promise<void> {
  const request = { gameId };
  const response = await Api.games().apiGamesGameIdStartRequestPost(request);
  const action = gameUpdated(response);
  store.dispatch(action);
  navigateTo(Routes.gamePlay(gameId));
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export async function getGameByInvite(code: string): Promise<any> {
  const state = store.getState();
  const apiUrl = state.config.environment.apiUrl;

  const response = await fetch(`${apiUrl}/api/games/invite/${code}`, {
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(response.status === 404 ? 'Game not found' : 'Failed to load game');
  }

  return response.json();
}

export async function joinByInvite(code: string): Promise<void> {
  const state = store.getState();
  const apiUrl = state.config.environment.apiUrl;

  const response = await fetch(`${apiUrl}/api/games/invite/${code}/join`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
  });

  if (!response.ok) {
    const text = await response.text();
    let message = `Failed to join game (${response.status})`;
    try {
      const problem = JSON.parse(text);
      if (problem.title) message = problem.title;
    } catch {
      // use default
    }
    throw new Error(message);
  }

  const data = await response.json();
  const action = gameUpdated(data);
  store.dispatch(action);
}
