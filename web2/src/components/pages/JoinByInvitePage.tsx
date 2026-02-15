import React, { FC, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Typography, Button, CircularProgress, Box, Paper } from '@mui/material';
import { useSelector } from 'react-redux';
import { selectSession } from '../../hooks/selectors';
import { getGameByInvite, joinByInvite } from '../../controllers/gameController';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';
import QuickJoinForm from '../forms/QuickJoinForm';
import { formStyles } from '../../styles/styles';

const JoinByInvitePage: FC = () => {
  const { code } = useParams<{ code: string }>();
  const session = useSelector(selectSession);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const [game, setGame] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [joining, setJoining] = useState(false);

  useEffect(() => {
    if (!code) return;
    setLoading(true);
    getGameByInvite(code)
      .then((g) => {
        setGame(g);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.message || 'Failed to load game');
        setLoading(false);
      });
  }, [code]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !game) {
    return (
      <div>
        <Typography variant="h4">Game Not Found</Typography>
        <Typography variant="body1" sx={{ mt: 2 }}>
          {error || 'This invite link is invalid or has expired.'}
        </Typography>
      </div>
    );
  }

  const handleJoin = async () => {
    if (!code) return;
    setJoining(true);
    try {
      await joinByInvite(code);
      navigateTo(Routes.gameLobby(game.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to join');
      setJoining(false);
    }
  };

  const isSignedIn = !!session.user;
  const playerCount = game.players?.length ?? 0;
  const maxPlayers = game.parameters?.regionCount ?? 0;

  return (
    <div>
      <Typography variant="h4">Game Invite</Typography>
      <Paper sx={{ p: 2, mt: 2, maxWidth: 400, mx: 'auto' }}>
        <Typography variant="body1">
          {game.parameters?.description || `Game #${game.id}`}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          {`${playerCount}/${maxPlayers} players`}
        </Typography>
        {game.parameters?.regionCount && (
          <Typography variant="body2" color="text.secondary">
            {`${game.parameters.regionCount} regions`}
          </Typography>
        )}
      </Paper>

      <Box sx={{ mt: 3 }}>
        {isSignedIn ? (
          <Button
            sx={formStyles.button}
            onClick={handleJoin}
            disabled={joining}
          >
            {joining ? 'Joining...' : 'Join Game'}
          </Button>
        ) : (
          <div>
            <Typography variant="body1" sx={{ mb: 2 }}>
              Pick a username to join this game:
            </Typography>
            <QuickJoinForm />
          </div>
        )}
      </Box>
    </div>
  );
};

export default JoinByInvitePage;
