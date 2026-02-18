import React, { FC, useEffect, useRef, useState, useCallback } from 'react';
import { useSelector } from 'react-redux';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';
import CheckIcon from '@mui/icons-material/Check';
import UndoIcon from '@mui/icons-material/Undo';
import FlagIcon from '@mui/icons-material/Flag';
import CancelIcon from '@mui/icons-material/Cancel';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';
import CanvasBoard from '../Canvas/CanvasBoard';
import { GamePageProps } from './GamePage';
import { selectActiveGame, selectSession, selectBoards, selectImages } from '../../hooks/selectors';
import { loadGame, cancelGame } from '../../controllers/gameController';
import { loadBoard } from '../../controllers/boardController';
import { preloadAllPieceImages, getFieldOfPowerImage } from '../../controllers/imageController';
import { selectCell, commitTurn, resetTurn } from '../../controllers/turnController';
import { concedePlayer } from '../../controllers/playerController';
import { SelectionKind } from '../../api-client';
import { fillEmptyBoardView } from '../../board/boardViewFactory';
import { getScale, getSize, transformBoardView, CanvasTranformData } from '../../board/canvasTransformService';
import { CellView } from '../../board/model';
import { GameStatus, TurnStatus, PlayerStatus } from '../../api-client';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';
import TurnTimer from '../game/TurnTimer';
import TurnOrderPanel from '../game/TurnOrderPanel';
import DebugPanel from '../game/DebugPanel';

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
  }, [imagesState.pieces.size]);

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
        canvasMargin: 80,
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

  const turn = game?.currentTurn;
  const currentPlayer =
    game && game.turnCycle && game.turnCycle.length > 0
      ? game.players?.find((p) => p.id === game.turnCycle![0])
      : undefined;
  const isMyTurn = currentPlayer && user && currentPlayer.userId === user.id;

  const hasSelections = (turn?.selections?.length ?? 0) > 0;
  const isAwaitingCommit = turn?.status === TurnStatus.AwaitingCommit;
  const showTurnActions = isMyTurn && hasSelections;

  const isCreator = game && user && game.createdBy.userId === user.id;
  const myPlayers =
    game && user
      ? (game.players?.filter(
          (p) => p.userId === user.id && p.status === PlayerStatus.Alive,
        ) ?? [])
      : [];
  const canConcede = !isSpectator && myPlayers.length > 0 && game?.status === GameStatus.InProgress;
  const canCancelGame =
    isCreator &&
    game &&
    (game.status === GameStatus.Pending || game.status === GameStatus.InProgress);

  const [concedeDialogOpen, setConcedeDialogOpen] = useState(false);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.code === 'Space' && isMyTurn && isAwaitingCommit && game) {
        e.preventDefault();
        commitTurn(game.id);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isMyTurn, isAwaitingCommit, game?.id]);

  const handleConcede = async () => {
    if (!game || myPlayers.length === 0) return;
    setConcedeDialogOpen(false);
    for (const p of myPlayers) {
      await concedePlayer(game.id, p.id);
    }
  };

  const handleCancelGame = async () => {
    if (!game) return;
    setCancelDialogOpen(false);
    await cancelGame(game.id);
    navigateTo(Routes.home);
  };

  if (!game || !filledBoard || !canvasStyle || imagesState.pieces.size === 0) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80vh' }}>
        <RedirectToSignInIfSignedOut />
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 64px)', p: 0, m: -2.5 }}>
      <RedirectToSignInIfSignedOut />

      {game.parameters.turnTimeLimitSeconds &&
        game.status === GameStatus.InProgress &&
        turn?.turnStartedAt && (
          <TurnTimer
            turnStartedAt={turn.turnStartedAt}
            turnTimeLimitSeconds={game.parameters.turnTimeLimitSeconds}
          />
        )}

      <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, py: 0.5 }}>
        {showTurnActions && (
          <>
            {isAwaitingCommit && (
              <Button
                variant="contained"
                color="success"
                size="small"
                startIcon={<CheckIcon />}
                onClick={() => commitTurn(game.id)}
              >
                Confirm
              </Button>
            )}
            <Button
              variant="outlined"
              color="error"
              size="small"
              startIcon={<UndoIcon />}
              onClick={() => resetTurn(game.id)}
            >
              Cancel
            </Button>
          </>
        )}
        {canConcede && (
          <Button
            variant="outlined"
            color="warning"
            size="small"
            startIcon={<FlagIcon />}
            onClick={() => setConcedeDialogOpen(true)}
          >
            Concede
          </Button>
        )}
        {canCancelGame && (
          <Button
            variant="outlined"
            color="error"
            size="small"
            startIcon={<CancelIcon />}
            onClick={() => setCancelDialogOpen(true)}
          >
            Stop Game
          </Button>
        )}
      </Box>

      <Box
        ref={containerRef}
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'flex',
          justifyContent: 'center',
          position: 'relative',
        }}
      >
        <TurnOrderPanel game={game} />
        <CanvasBoard
          game={game}
          board={filledBoard}
          selectCell={handleSelectCell}
          style={canvasStyle}
          pieceImages={imagesState.pieces}
          fieldOfPowerImage={getFieldOfPowerImage()}
        />
      </Box>

      <Dialog open={concedeDialogOpen} onClose={() => setConcedeDialogOpen(false)}>
        <DialogTitle>Concede?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Your pieces will be abandoned and you will be removed from the game. This cannot be
            undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConcedeDialogOpen(false)}>No</Button>
          <Button onClick={handleConcede} color="warning" variant="contained">
            Concede
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={cancelDialogOpen} onClose={() => setCancelDialogOpen(false)}>
        <DialogTitle>Stop Game?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This will cancel the game for all players. This cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelDialogOpen(false)}>No</Button>
          <Button onClick={handleCancelGame} color="error" variant="contained">
            Stop Game
          </Button>
        </DialogActions>
      </Dialog>
      <DebugPanel game={game} />
    </Box>
  );
};

export default GamePlayPage;
