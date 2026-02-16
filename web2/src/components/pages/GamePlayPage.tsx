import React, { FC, useEffect, useRef, useState, useCallback } from 'react';
import { useSelector } from 'react-redux';
import { Box, Button, Typography, Paper, CircularProgress } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import CanvasBoard from '../Canvas/CanvasBoard';
import { GamePageProps } from './GamePage';
import { selectActiveGame, selectSession, selectBoards, selectImages } from '../../hooks/selectors';
import { loadGame } from '../../controllers/gameController';
import { loadBoard } from '../../controllers/boardController';
import { preloadAllPieceImages } from '../../controllers/imageController';
import { selectCell, commitTurn, resetTurn } from '../../controllers/turnController';
import { SelectionKind } from '../../api-client';
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
    async (cell: CellView) => {
      if (!game || isSpectator) return;
      const turn = game.currentTurn;
      if (!turn) return;

      const isValidSelection = turn.selectionOptions?.includes(cell.id);

      if (isValidSelection) {
        // Normal selection — send to API
        selectCell(game.id, cell.id);
        return;
      }

      // Not a valid selection — check if we should reset and re-select a different piece
      if (
        turn.requiredSelectionKind === SelectionKind.Move &&
        turn.selections?.length > 0 &&
        cell.piece
      ) {
        // Clicked a piece while choosing a move destination — switch to that piece
        await resetTurn(game.id);
        selectCell(game.id, cell.id);
      }
      // Otherwise ignore — clicked an empty/invalid cell
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [game?.id, game?.currentTurn, isSpectator],
  );

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

  // Auto-commit when turn reaches AwaitingCommit
  useEffect(() => {
    if (game && isMyTurn && turn?.status === TurnStatus.AwaitingCommit) {
      commitTurn(game.id);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [game?.id, isMyTurn, turn?.status]);

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

      {isMyTurn && game.status === GameStatus.InProgress && turn?.selections && turn.selections.length > 0 && (
        <Paper sx={{ p: 1, mt: 1 }} elevation={1}>
          <Button variant="outlined" onClick={handleReset}>
            Reset Turn
          </Button>
        </Paper>
      )}
    </Box>
  );
};

export default GamePlayPage;
