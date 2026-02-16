import React, { FC, useEffect, useRef, useState, useCallback } from 'react';
import { useSelector } from 'react-redux';
import { Box, Button, Typography, Paper, Stack, CircularProgress } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import CanvasBoard from '../Canvas/CanvasBoard';
import { GamePageProps } from './GamePage';
import { selectActiveGame, selectSession, selectBoards, selectImages } from '../../hooks/selectors';
import { loadGame } from '../../controllers/gameController';
import { loadBoard } from '../../controllers/boardController';
import { preloadAllPieceImages } from '../../controllers/imageController';
import { selectCell, commitTurn, resetTurn } from '../../controllers/turnController';
import { fillEmptyBoardView } from '../../board/boardViewFactory';
import { getScale, getSize, transformBoardView, CanvasTranformData } from '../../board/canvasTransformService';
import { CellView } from '../../board/model';
import { GameStatus, TurnStatus } from '../../api-client';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const GamePlayPage: FC<GamePageProps> = ({ gameId }) => {
  const { game } = useSelector(selectActiveGame);
  const session = useSelector(selectSession);
  const boardsState = useSelector(selectBoards);
  const imagesState = useSelector(selectImages);

  const containerRef = useRef<HTMLDivElement>(null);
  const [containerSize, setContainerSize] = useState({ x: 800, y: 600 });

  useEffect(() => {
    if (game?.id !== gameId) {
      loadGame(gameId);
    } else if (![GameStatus.InProgress, GameStatus.Over].includes(game.status)) {
      navigateTo(Routes.game(gameId));
    }
  }, [game?.id, game?.status, gameId]);

  useEffect(() => {
    if (game && game.parameters?.regionCount) {
      const rc = game.parameters.regionCount;
      if (!boardsState.emptyBoardViews.has(rc)) {
        loadBoard(rc);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.parameters?.regionCount, boardsState.emptyBoardViews]);

  useEffect(() => {
    if (imagesState.pieces.size === 0) {
      preloadAllPieceImages();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const ro = new ResizeObserver((entries) => {
      const { width, height } = entries[0].contentRect;
      if (width > 0 && height > 0) {
        setContainerSize({ x: width, y: height });
      }
    });
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const emptyBoard = game
    ? boardsState.emptyBoardViews.get(game.parameters.regionCount)
    : undefined;
  const user = session?.user;
  const isSpectator =
    game && user ? !game.players?.some((p) => p.userId === user.id) : false;

  const transformData: CanvasTranformData | undefined = game
    ? {
        containerSize,
        canvasMargin: 10,
        contentPadding: 5,
        regionCount: game.parameters.regionCount,
        zoomLevel: 0,
      }
    : undefined;

  const filledBoard =
    emptyBoard && game && user && transformData
      ? transformBoardView(fillEmptyBoardView(emptyBoard, game, user), transformData)
      : undefined;

  const canvasStyle = transformData
    ? (() => {
        const scale = getScale(transformData);
        const size = getSize(transformData);
        return { width: size.x, height: size.y, scale };
      })()
    : undefined;

  const handleSelectCell = useCallback(
    (cell: CellView) => {
      if (game && !isSpectator) {
        selectCell(game.id, cell.id);
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [game?.id, isSpectator],
  );

  const handleCommit = useCallback(() => {
    if (game) commitTurn(game.id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.id]);

  const handleReset = useCallback(() => {
    if (game) resetTurn(game.id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.id]);

  const turn = game?.currentTurn;
  const currentPlayer =
    game && game.turnCycle && game.turnCycle.length > 0
      ? game.players?.find((p) => p.id === game.turnCycle![0])
      : undefined;
  const isMyTurn = currentPlayer && user && currentPlayer.userId === user.id;

  if (!game || !filledBoard || !canvasStyle || imagesState.pieces.size === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh' }}>
        <RedirectToSignInIfSignedOut />
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', p: 1 }}>
      <RedirectToSignInIfSignedOut />

      <Paper sx={{ p: 1, mb: 1 }} elevation={1}>
        <Typography variant="subtitle1">
          {game.status === GameStatus.Over
            ? 'Game Over'
            : currentPlayer
              ? `${currentPlayer.name}'s turn${isMyTurn ? ' (You)' : ''}`
              : 'Waiting...'}
          {isSpectator && ' — Watching'}
        </Typography>
        {turn?.requiredSelectionKind && isMyTurn && (
          <Typography variant="body2" color="text.secondary">
            {`Select: ${turn.requiredSelectionKind}`}
          </Typography>
        )}
      </Paper>

      <Box
        ref={containerRef}
        sx={{
          flex: 1,
          minHeight: 300,
          display: 'flex',
          justifyContent: 'center',
          overflow: 'auto',
        }}
      >
        <CanvasBoard
          game={game}
          board={filledBoard}
          selectCell={handleSelectCell}
          style={canvasStyle}
          pieceImages={imagesState.pieces}
        />
      </Box>

      {isMyTurn && game.status === GameStatus.InProgress && (
        <Paper sx={{ p: 1, mt: 1 }} elevation={1}>
          <Stack direction="row" spacing={1}>
            {turn?.status === TurnStatus.AwaitingCommit && (
              <Button variant="contained" color="primary" onClick={handleCommit}>
                Commit Turn
              </Button>
            )}
            {turn?.selections && turn.selections.length > 0 && (
              <Button variant="outlined" onClick={handleReset}>
                Reset Turn
              </Button>
            )}
          </Stack>
        </Paper>
      )}
    </Box>
  );
};

export default GamePlayPage;
