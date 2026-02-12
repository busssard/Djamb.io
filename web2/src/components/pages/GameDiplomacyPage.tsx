import React, { FC, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { Typography } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import { GamePageProps } from './GamePage';
import { loadGame } from '../../controllers/gameController';
import { selectActiveGame } from '../../hooks/selectors';
import { GameStatus } from '../../api-client';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const GameDiplomacyPage: FC<GamePageProps> = ({ gameId }) => {
  const { game } = useSelector(selectActiveGame);

  useEffect(() => {
    if (game?.id !== gameId) {
      loadGame(gameId);
    } else if (game.status !== GameStatus.InProgress) {
      navigateTo(Routes.game(gameId));
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.id, game?.status, gameId]);

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4">
        {`Game ${gameId} diplomacy page`}
      </Typography>
      <br />
      {game ? JSON.stringify(game) : ''}
    </div>
  );
};

export default GameDiplomacyPage;
