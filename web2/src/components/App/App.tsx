import React, { FC, useEffect, Suspense } from 'react';
import { BrowserRouter, Routes, Route, useParams } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { CircularProgress } from '@mui/material';
import { makeStyles } from '@mui/styles';
import NavigationDrawer from '../NavigationDrawer/NavigationDrawer';
import { loadConfig } from '../../controllers/configController';
import * as RoutePaths from '../../utilities/routes';
import RedirectBasedOnStore from '../routing/RedirectBasedOnStore';
import { restoreSession } from '../../controllers/userController';
import TopBar from '../TopBar/TopBar';
import { theme } from '../../styles/materialTheme';
import LatestNotificationSnackbar from '../notifications/LatestNotificationSnackBar';
import { loadGame, blockGameLoading } from '../../controllers/gameController';

// Lazy-loaded page components
const NoMatchPage = React.lazy(() => import('../pages/NoMatchPage'));
const CreateAccountPage = React.lazy(() => import('../pages/CreateAccountPage'));
const SignInPage = React.lazy(() => import('../pages/SignInPage'));
const UserConfigPage = React.lazy(() => import('../pages/UserConfigPage'));
const RulesPage = React.lazy(() => import('../pages/RulesPage'));
const GameDiplomacyPage = React.lazy(() => import('../pages/GameDiplomacyPage'));
const GameLobbyPage = React.lazy(() => import('../pages/GameLobbyPage'));
const GameOutcomePage = React.lazy(() => import('../pages/GameOutcomePage'));
const GameSnapshotsPage = React.lazy(() => import('../pages/GameSnapshotsPage'));
const GamePage = React.lazy(() => import('../pages/GamePage'));
const GamePlayPage = React.lazy(() => import('../pages/GamePlayPage'));
const CreateGamePage = React.lazy(() => import('../pages/CreateGamePage'));
const HomePage = React.lazy(() => import('../pages/HomePage'));
const SearchGamesPage = React.lazy(() => import('../pages/SearchGamesPage'));
const SignOutPage = React.lazy(() => import('../pages/SignOutPage'));
const GameInfoPage = React.lazy(() => import('../pages/GameInfoPage'));
const NotificationsPage = React.lazy(() => import('../pages/NotificationsPage'));

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
      loadConfig().then(() => restoreSession());
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
          <Suspense fallback={<CircularProgress />}>
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
          </Suspense>
        </div>
      </BrowserRouter>
    </ThemeProvider>
  );
};

export default App;
