import React, { FC, useState, useEffect } from 'react';
import { Typography, Container } from '@mui/material';
import { useSelector } from 'react-redux';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import GameSearchResultsTable from '../tables/GameSearchResultsTable';
import { SearchGameDto, GameStatus } from '../../api-client';
import { searchGames } from '../../controllers/searchController';
import { selectSession } from '../../hooks/selectors';

const HomePage: FC = () => {
  const [recentGames, setRecentGames] = useState<SearchGameDto[]>([]);
  const { user } = useSelector(selectSession);

  useEffect(() => {
    searchGames({
      playerUserName: user?.name,
      statuses: [GameStatus.Pending, GameStatus.InProgress],
    }).then((games) => {
      const sorted = games.sort((x) => x.id).reverse();
      setRecentGames(sorted);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.name]);

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4">Home</Typography>
      <br />
      <br />
      <Typography variant="h5">Recent games</Typography>
      <br />
      <Container maxWidth="md">
        <GameSearchResultsTable games={recentGames} />
      </Container>
    </div>
  );
};

export default HomePage;
