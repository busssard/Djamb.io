import { store } from '../redux';
import { gameUpdated } from '../redux/activeGame/actionFactory';
import { PlayerStatus } from '../api-client';
import * as Api from '../utilities/api';

export async function concedePlayer(gameId: number, playerId: number): Promise<void> {
  const response = await Api.players().apiGamesGameIdPlayersPlayerIdStatusStatusPut({
    gameId,
    playerId,
    status: PlayerStatus.Conceded,
  });
  store.dispatch(gameUpdated(response));
}
