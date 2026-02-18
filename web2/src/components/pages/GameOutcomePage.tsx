import React, { FC, useEffect } from 'react';
import { useSelector } from 'react-redux';
import { Box, Button, Paper, Typography } from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import { GamePageProps } from './GamePage';
import { selectActiveGame } from '../../hooks/selectors';
import { loadGame } from '../../controllers/gameController';
import { GameStatus, PlayerStatus, PlayerDto } from '../../api-client';
import { navigateTo } from '../../controllers/navigationController';
import { pieceColors } from '../../styles/styles';
import * as Routes from '../../utilities/routes';

function getStatusRank(status: PlayerStatus): number {
  switch (status) {
    case PlayerStatus.Victorious:
      return 0;
    case PlayerStatus.Alive:
      return 1;
    case PlayerStatus.AcceptsDraw:
      return 2;
    case PlayerStatus.WillConcede:
      return 3;
    case PlayerStatus.Eliminated:
      return 4;
    case PlayerStatus.Conceded:
      return 5;
    default:
      return 6;
  }
}

function getStatusLabel(status: PlayerStatus): string {
  switch (status) {
    case PlayerStatus.Victorious:
      return 'Victory';
    case PlayerStatus.Alive:
      return 'Survived';
    case PlayerStatus.Eliminated:
      return 'Eliminated';
    case PlayerStatus.Conceded:
      return 'Conceded';
    default:
      return status;
  }
}

function rankPlayers(players: PlayerDto[]): PlayerDto[] {
  return [...players]
    .filter((p) => p.startingRegion != null)
    .sort((a, b) => getStatusRank(a.status) - getStatusRank(b.status));
}

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

  if (!game || game.status !== GameStatus.Over) {
    return (
      <div>
        <RedirectToSignInIfSignedOut />
      </div>
    );
  }

  const ranked = rankPlayers(game.players);

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: 'calc(100vh - 64px)',
      }}
    >
      <RedirectToSignInIfSignedOut />
      <Paper
        elevation={6}
        sx={{
          p: 4,
          minWidth: 320,
          maxWidth: 480,
          textAlign: 'center',
          bgcolor: 'background.paper',
        }}
      >
        <Typography variant="h3" sx={{ mb: 3, fontWeight: 'bold' }}>
          Game Over
        </Typography>

        <Box sx={{ mb: 3 }}>
          {ranked.map((player, index) => {
            const color = pieceColors.getPlayer(player.colorId ?? null);
            const isWinner = player.status === PlayerStatus.Victorious;
            return (
              <Box
                key={player.id}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  py: 1,
                  px: 2,
                  mb: 0.5,
                  borderRadius: 1,
                  bgcolor: isWinner ? 'rgba(255, 215, 0, 0.1)' : 'transparent',
                  border: isWinner ? '1px solid rgba(255, 215, 0, 0.4)' : '1px solid transparent',
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                  <Typography
                    variant="h6"
                    sx={{ fontWeight: 'bold', color: 'text.secondary', minWidth: 28 }}
                  >
                    #{index + 1}
                  </Typography>
                  <Box
                    sx={{
                      width: 12,
                      height: 12,
                      borderRadius: '50%',
                      bgcolor: color,
                      flexShrink: 0,
                    }}
                  />
                  <Typography
                    variant="body1"
                    sx={{ fontWeight: isWinner ? 'bold' : 'normal', color }}
                  >
                    {player.name}
                  </Typography>
                </Box>
                <Typography
                  variant="caption"
                  sx={{ color: 'text.secondary', fontStyle: 'italic' }}
                >
                  {getStatusLabel(player.status)}
                </Typography>
              </Box>
            );
          })}
        </Box>

        <Button
          variant="contained"
          startIcon={<HomeIcon />}
          onClick={() => navigateTo(Routes.home)}
          sx={{ mt: 1 }}
        >
          Back to Home
        </Button>
      </Paper>
    </Box>
  );
};

export default GameOutcomePage;
