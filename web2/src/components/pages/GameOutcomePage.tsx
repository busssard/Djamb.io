import React, { FC, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { Typography } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import { GamePageProps } from './GamePage';
import { selectActiveGame } from '../../hooks/selectors';
import { loadGame } from '../../controllers/gameController';
import { GameStatus } from '../../api-client';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const GameOutcomePage: FC<GamePageProps> = ({ gameId }) => {
  const { game } = useSelector(selectActiveGame);

  useEffect(() => {
    if (game?.id !== gameId) {
      loadGame(gameId);
    } else if (game.status !== GameStatus.Over) {
      navigateTo(Routes.game(gameId));
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.id, game?.status, gameId]);

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4">
        {`Game ${gameId} outcome page`}
      </Typography>
      <br />
      {game ? JSON.stringify(game) : ''}
    </div>
  );
};

export default GameOutcomePage;
