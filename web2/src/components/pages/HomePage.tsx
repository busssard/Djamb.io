import React, { FC, useState, useEffect } from 'react';
import { Typography, Container, Button, Box } from '@mui/material';
import { useSelector } from 'react-redux';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import GameSearchResultsTable from '../tables/GameSearchResultsTable';
import { SearchGameDto, GameStatus } from '../../api-client';
import { searchGames } from '../../controllers/searchController';
import { selectSession } from '../../hooks/selectors';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const HomePage: FC = () => {
  const [myGames, setMyGames] = useState<SearchGameDto[]>([]);
  const [publicGames, setPublicGames] = useState<SearchGameDto[]>([]);
  const { user } = useSelector(selectSession);

  useEffect(() => {
    if (!user?.name) return;

    searchGames({
      playerUserName: user.name,
      statuses: [GameStatus.Pending, GameStatus.InProgress],
    }).then((games) => {
      setMyGames(games.sort((a, b) => b.id - a.id));
    });

    searchGames({
      isPublic: true,
      statuses: [GameStatus.Pending, GameStatus.InProgress],
    }).then((games) => {
      const notMine = games.filter((g) => !g.containsMe);
      setPublicGames(notMine.sort((a, b) => b.id - a.id));
    });
  }, [user?.name]);

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
        <Typography variant="h4">Home</Typography>
        <Button variant="contained" onClick={() => navigateTo(Routes.newGame)}>
          Create Game
        </Button>
      </Box>

      <Typography variant="h5" sx={{ mt: 2, mb: 1 }}>
        Your Active Games
      </Typography>
      <Container maxWidth="md">
        {myGames.length === 0 ? (
          <Typography color="text.secondary" sx={{ py: 2 }}>
            No active games. Create one or join a public game below.
          </Typography>
        ) : (
          <GameSearchResultsTable games={myGames} />
        )}
      </Container>

      <Typography variant="h5" sx={{ mt: 4, mb: 1 }}>
        Public Games
      </Typography>
      <Container maxWidth="md">
        {publicGames.length === 0 ? (
          <Typography color="text.secondary" sx={{ py: 2 }}>
            No open public games right now.
          </Typography>
        ) : (
          <GameSearchResultsTable games={publicGames} />
        )}
      </Container>
    </div>
  );
};

export default HomePage;
