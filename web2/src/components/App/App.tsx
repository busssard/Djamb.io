import React, {
  FC, useEffect,
} from 'react';
import {
  BrowserRouter, Routes, Route, useParams,
} from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { makeStyles } from '@mui/styles';
import NavigationDrawer from '../NavigationDrawer/NavigationDrawer';
import { loadConfig } from '../../controllers/configController';
import * as RoutePaths from '../../utilities/routes';
import NoMatchPage from '../pages/NoMatchPage';
import RedirectBasedOnStore from '../routing/RedirectBasedOnStore';
import CreateAccountPage from '../pages/CreateAccountPage';
import SignInPage from '../pages/SignInPage';
import UserConfigPage from '../pages/UserConfigPage';
import { restoreSession } from '../../controllers/userController';
import RulesPage from '../pages/RulesPage';
import GameDiplomacyPage from '../pages/GameDiplomacyPage';
import GameLobbyPage from '../pages/GameLobbyPage';
import GameOutcomePage from '../pages/GameOutcomePage';
import GameSnapshotsPage from '../pages/GameSnapshotsPage';
import GamePage from '../pages/GamePage';
import GamePlayPage from '../pages/GamePlayPage';
import CreateGamePage from '../pages/CreateGamePage';
import HomePage from '../pages/HomePage';
import SearchGamesPage from '../pages/SearchGamesPage';
import TopBar from '../TopBar/TopBar';
import SignOutPage from '../pages/SignOutPage';
import { theme } from '../../styles/materialTheme';
import GameInfoPage from '../pages/GameInfoPage';
import NotificationsPage from '../pages/NotificationsPage';
import LatestNotificationSnackbar from '../notifications/LatestNotificationSnackBar';
import { loadGame, blockGameLoading } from '../../controllers/gameController';

const useStyles = makeStyles({
  page: {
    textAlign: 'center',
    padding: '20px',
    background: '#161616',
  },
});

const getGameId = (): number | null => {
  const url = window.location.href;
  const regex = new RegExp('.*games/(\\d+).*');
  const match = regex.exec(url);
  const gameId = match?.[1];
  return gameId ? Number(gameId) : null;
};

function useGameId(): number {
  const { gameId } = useParams();
  return Number(gameId);
}

const GameDiplomacyRoute: FC = () => <GameDiplomacyPage gameId={useGameId()} />;
const GameInfoRoute: FC = () => <GameInfoPage gameId={useGameId()} />;
const GameLobbyRoute: FC = () => <GameLobbyPage gameId={useGameId()} />;
const GameOutcomeRoute: FC = () => <GameOutcomePage gameId={useGameId()} />;
const GamePlayRoute: FC = () => <GamePlayPage gameId={useGameId()} />;
const GameSnapshotsRoute: FC = () => <GameSnapshotsPage gameId={useGameId()} />;
const GameRoute: FC = () => <GamePage gameId={useGameId()} />;

const App: FC = () => {
  useEffect(() => {
    // All API calls must happen after config is loaded, because that sets the API URL.

    const gameId = getGameId();

    if (gameId) {
      // If the URL has a gameID when the app first loads, load that game.
      // Game pages will also load the game because of in-app navigation.
      // First block the pages from redundantly loading the game.
      blockGameLoading();
      loadConfig()
        .then(() => restoreSession())
        .then(() => loadGame(gameId, true));
    } else {
      loadConfig()
        .then(() => restoreSession());
    }
  }, []);

  const classes = useStyles();

  return (
    <ThemeProvider theme={theme}>
      <BrowserRouter>
        <RedirectBasedOnStore />
        <NavigationDrawer />
        <LatestNotificationSnackbar />
        <TopBar />
        <div className={classes.page}>
          <Routes>
            {/* Gameless pages */}
            <Route path={RoutePaths.settings} element={<UserConfigPage />} />
            <Route path={RoutePaths.signIn} element={<SignInPage />} />
            <Route path={RoutePaths.signOut} element={<SignOutPage />} />
            <Route path={RoutePaths.createAccount} element={<CreateAccountPage />} />
            <Route path={RoutePaths.rules} element={<RulesPage />} />
            <Route path={RoutePaths.home} element={<HomePage />} />
            <Route path={RoutePaths.notifications} element={<NotificationsPage />} />
            <Route path={RoutePaths.newGame} element={<CreateGamePage />} />
            <Route path={RoutePaths.searchGames} element={<SearchGamesPage />} />
            {/* Active game pages */}
            <Route path={RoutePaths.gameDiplomacyTemplate} element={<GameDiplomacyRoute />} />
            <Route path={RoutePaths.gameInfoTemplate} element={<GameInfoRoute />} />
            <Route path={RoutePaths.gameLobbyTemplate} element={<GameLobbyRoute />} />
            <Route path={RoutePaths.gameOutcomeTemplate} element={<GameOutcomeRoute />} />
            <Route path={RoutePaths.gamePlayTemplate} element={<GamePlayRoute />} />
            <Route path={RoutePaths.gameSnapshotsTemplate} element={<GameSnapshotsRoute />} />
            <Route path={RoutePaths.gameTemplate} element={<GameRoute />} />
            {/* Misc pages */}
            <Route path="*" element={<NoMatchPage />} />
          </Routes>
        </div>
      </BrowserRouter>
    </ThemeProvider>
  );
};

export default App;
