import { store } from '../redux';
import { gameUpdated } from '../redux/activeGame/actionFactory';
import * as Api from '../utilities/api';

export async function selectCell(gameId: number, cellId: number): Promise<void> {
  const request = { gameId, cellId };
  const response = await Api.turns().apiGamesGameIdCurrentTurnSelectionRequestCellIdPost(request);
  const action = gameUpdated(response);
  store.dispatch(action);
}

export async function commitTurn(gameId: number): Promise<void> {
  const response = await Api.turns().apiGamesGameIdCurrentTurnCommitRequestPost({ gameId });
  store.dispatch(gameUpdated(response));
}

export async function resetTurn(gameId: number): Promise<void> {
  const response = await Api.turns().apiGamesGameIdCurrentTurnResetRequestPost({ gameId });
  store.dispatch(gameUpdated(response));
}
