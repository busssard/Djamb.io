import React, { FC, useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import { Typography, Container, Button, Paper, Snackbar } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import { GamePageProps } from './GamePage';
import { selectActiveGame } from '../../hooks/selectors';
import { loadGame, startGame } from '../../controllers/gameController';
import { GameStatus } from '../../api-client';
import LobbyPlayersTable from '../tables/LobbyPlayersTable/LobbyPlayersTable';
import GameParametersTable from '../tables/GameParametersTable';
import { formStyles } from '../../styles/styles';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const GameLobbyPage: FC<GamePageProps> = ({ gameId }) => {
  const { game } = useSelector(selectActiveGame);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (game?.id !== gameId) {
      loadGame(gameId);
    }
  }, [game?.id, gameId]);

  if (game === null) {
    return <></>;
  }

  if (game.status !== GameStatus.Pending) {
    navigateTo(Routes.gameInfo(game.id));
    return <></>;
  }

  const canStart = game.players.length >= 2;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const inviteCode = (game as any).inviteCode as string | null;
  const inviteUrl = inviteCode ? `${window.location.origin}/invite/${inviteCode}` : null;

  const copyInviteLink = () => {
    if (inviteUrl) {
      navigator.clipboard.writeText(inviteUrl);
      setCopied(true);
    }
  };

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4">{`Game ${gameId} lobby`}</Typography>

      {inviteUrl && (
        <Paper sx={{ p: 2, mt: 2, mb: 2, maxWidth: 500, mx: 'auto' }}>
          <Typography variant="subtitle2" color="text.secondary">
            Invite friends with this link:
          </Typography>
          <Typography
            variant="body2"
            sx={{ fontFamily: 'monospace', mt: 0.5, wordBreak: 'break-all' }}
          >
            {inviteUrl}
          </Typography>
          <Button size="small" onClick={copyInviteLink} sx={{ mt: 1 }}>
            Copy Link
          </Button>
          <Snackbar
            open={copied}
            autoHideDuration={2000}
            onClose={() => setCopied(false)}
            message="Link copied!"
          />
        </Paper>
      )}

      <br />
      <Container maxWidth="xs">
        <Typography variant="h5">Settings</Typography>
        <GameParametersTable />
      </Container>
      <br />
      <br />
      <Container maxWidth="sm">
        <Typography variant="h5">
          {`Players (${game.players.length}/${game.parameters.regionCount})`}
        </Typography>
        <LobbyPlayersTable />
      </Container>
      <br />
      <br />
      <Container maxWidth="xs">
        <Button sx={formStyles.button} onClick={() => startGame(game.id)} disabled={!canStart}>
          Start
        </Button>
        <br />
        <br />
        {canStart ? (
          <></>
        ) : (
          <Typography variant="caption">Cannot start until more players join.</Typography>
        )}
      </Container>
    </div>
  );
};

export default GameLobbyPage;
