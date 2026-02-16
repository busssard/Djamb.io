import React, { FC, useState, useEffect } from 'react';
import { Typography, Container, ToggleButtonGroup, ToggleButton, Box } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import GameSearchResultsTable from '../tables/GameSearchResultsTable';
import { SearchGameDto, GameStatus } from '../../api-client';
import { searchGames } from '../../controllers/searchController';

type StatusFilter = 'open' | 'live' | 'all';

const SearchGamesPage: FC = () => {
  const [games, setGames] = useState<SearchGameDto[]>([]);
  const [filter, setFilter] = useState<StatusFilter>('open');

  useEffect(() => {
    const statuses =
      filter === 'open'
        ? [GameStatus.Pending]
        : filter === 'live'
          ? [GameStatus.InProgress]
          : [GameStatus.Pending, GameStatus.InProgress];

    searchGames({
      isPublic: true,
      statuses,
    }).then((results) => {
      setGames(results.sort((a, b) => b.id - a.id));
    });
  }, [filter]);

  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4" sx={{ mb: 2 }}>
        Browse Games
      </Typography>

      <Box sx={{ mb: 2 }}>
        <ToggleButtonGroup
          value={filter}
          exclusive
          onChange={(_, val) => val && setFilter(val)}
          size="small"
        >
          <ToggleButton value="open">Open</ToggleButton>
          <ToggleButton value="live">In Progress</ToggleButton>
          <ToggleButton value="all">All</ToggleButton>
        </ToggleButtonGroup>
      </Box>

      <Container maxWidth="md">
        {games.length === 0 ? (
          <Typography color="text.secondary" sx={{ py: 2 }}>
            No {filter === 'open' ? 'open' : filter === 'live' ? 'in-progress' : ''} public games
            found.
          </Typography>
        ) : (
          <GameSearchResultsTable games={games} />
        )}
      </Container>
    </div>
  );
};

export default SearchGamesPage;
