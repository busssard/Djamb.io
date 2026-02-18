import React, { FC, useState } from 'react';
import { Box, IconButton, Paper, Typography, Collapse } from '@mui/material';
import BugReportIcon from '@mui/icons-material/BugReport';
import { GameDto, PlayerStatus, PieceKind } from '../../api-client';
import { pieceColors } from '../../styles/styles';

interface Props {
  game: GameDto;
}

function pieceKindLabel(kind: PieceKind): string {
  switch (kind) {
    case PieceKind.Conduit: return 'Chief';
    case PieceKind.Thug: return 'Thug';
    case PieceKind.Scientist: return 'Scientist';
    case PieceKind.Hunter: return 'Hunter';
    case PieceKind.Diplomat: return 'Diplomat';
    case PieceKind.Reaper: return 'Reaper';
    case PieceKind.Corpse: return 'Corpse';
    default: return String(kind);
  }
}

const DebugPanel: FC<Props> = ({ game }) => {
  const [open, setOpen] = useState(false);

  const currentPlayerId = game.turnCycle?.[0] ?? null;
  const currentPlayer = game.players.find((p) => p.id === currentPlayerId) ?? null;
  const turn = game.currentTurn;
  const pieces = game.pieces ?? [];

  const alivePlayers = game.players.filter(
    (p) => p.status === PlayerStatus.Alive || p.status === PlayerStatus.AcceptsDraw || p.status === PlayerStatus.WillConcede,
  );

  const piecesByPlayer = new Map<number | null, typeof pieces>();
  for (const piece of pieces) {
    const key = piece.playerId ?? null;
    if (!piecesByPlayer.has(key)) {
      piecesByPlayer.set(key, []);
    }
    piecesByPlayer.get(key)!.push(piece);
  }

  return (
    <Box sx={{ position: 'fixed', bottom: 8, right: 8, zIndex: 1000 }}>
      <IconButton
        onClick={() => setOpen(!open)}
        sx={{
          position: 'absolute',
          bottom: 0,
          right: 0,
          bgcolor: open ? 'primary.main' : 'rgba(0,0,0,0.5)',
          color: 'white',
          '&:hover': { bgcolor: 'primary.dark' },
        }}
        size="small"
      >
        <BugReportIcon fontSize="small" />
      </IconButton>
      <Collapse in={open}>
        <Paper
          elevation={8}
          sx={{
            p: 1.5,
            mb: 5,
            maxHeight: '60vh',
            overflow: 'auto',
            minWidth: 300,
            maxWidth: 400,
            bgcolor: 'rgba(0,0,0,0.9)',
            fontFamily: 'monospace',
            fontSize: 11,
          }}
        >
          <Typography variant="caption" sx={{ color: 'grey.500', display: 'block', mb: 0.5 }}>
            Bot Interface Debug View
          </Typography>

          <Section label="Game">
            <Line label="id" value={game.id} />
            <Line label="status" value={game.status} />
            <Line label="ruleset" value={game.parameters.rulesetKind === 1 ? 'TotalWar' : 'Classic'} />
            <Line label="regions" value={game.parameters.regionCount} />
          </Section>

          <Section label="Turn Cycle">
            <Row>
              {game.turnCycle?.map((pid, i) => {
                const p = game.players.find((pl) => pl.id === pid);
                const color = pieceColors.getPlayer(p?.colorId ?? null);
                return (
                  <Box
                    key={i}
                    component="span"
                    sx={{
                      color,
                      fontWeight: i === 0 ? 'bold' : 'normal',
                      textDecoration: i === 0 ? 'underline' : 'none',
                    }}
                  >
                    {p?.name ?? `P${pid}`}
                    {i < (game.turnCycle?.length ?? 0) - 1 ? ' → ' : ''}
                  </Box>
                );
              }) ?? <span style={{ color: '#888' }}>none</span>}
            </Row>
          </Section>

          <Section label="Current Turn">
            {turn ? (
              <>
                <Line label="player" value={currentPlayer?.name ?? 'none'} />
                <Line label="status" value={turn.status ?? 'unknown'} />
                <Line label="requiredSelection" value={turn.requiredSelectionKind ?? 'none'} />
                <Line label="selectionOptions" value={`[${turn.selectionOptions?.join(', ') ?? ''}]`} />
                <Line label="optionCount" value={turn.selectionOptions?.length ?? 0} />
                {turn.selections?.length > 0 && (
                  <Box sx={{ mt: 0.5 }}>
                    <Typography variant="caption" sx={{ color: 'grey.400' }}>selections:</Typography>
                    {turn.selections.map((s, i) => (
                      <Row key={i}>
                        <span style={{ color: '#aaa' }}>{s.kind}</span>
                        {' → cell '}
                        <span style={{ color: '#4fc3f7' }}>{s.cellId}</span>
                        {s.pieceId != null && (
                          <span style={{ color: '#aaa' }}> (piece {s.pieceId})</span>
                        )}
                      </Row>
                    ))}
                  </Box>
                )}
              </>
            ) : (
              <Row><span style={{ color: '#888' }}>no active turn</span></Row>
            )}
          </Section>

          <Section label="Players">
            {alivePlayers.map((p) => {
              const color = pieceColors.getPlayer(p.colorId ?? null);
              const playerPieces = piecesByPlayer.get(p.id) ?? [];
              const piecesSummary = playerPieces
                .filter((pc) => pc.kind !== PieceKind.Corpse)
                .map((pc) => pieceKindLabel(pc.kind))
                .sort()
                .join(', ');
              return (
                <Box key={p.id} sx={{ mb: 0.5 }}>
                  <Row>
                    <span style={{ color, fontWeight: 'bold' }}>{p.name}</span>
                    <span style={{ color: '#888' }}> (id:{p.id}, status:{p.status})</span>
                  </Row>
                  <Row>
                    <span style={{ color: '#aaa', marginLeft: 8 }}>
                      {playerPieces.length} pieces: {piecesSummary || 'none'}
                    </span>
                  </Row>
                </Box>
              );
            })}
          </Section>

          <Section label="Pieces (all)">
            <Box sx={{ maxHeight: 150, overflow: 'auto' }}>
              {pieces.map((p) => {
                const owner = game.players.find((pl) => pl.id === p.playerId);
                const color = pieceColors.getPlayer(owner?.colorId ?? null);
                return (
                  <Row key={p.id}>
                    <span style={{ color: '#888' }}>#{p.id}</span>
                    {' '}
                    <span style={{ color }}>{pieceKindLabel(p.kind)}</span>
                    {' @ cell '}
                    <span style={{ color: '#4fc3f7' }}>{p.cellId}</span>
                    <span style={{ color: '#888' }}>
                      {' '}(owner:{p.playerId ?? 'none'}, orig:{p.originalPlayerId})
                    </span>
                  </Row>
                );
              })}
            </Box>
          </Section>
        </Paper>
      </Collapse>
    </Box>
  );
};

const Section: FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => (
  <Box sx={{ mb: 1 }}>
    <Typography
      variant="caption"
      sx={{ color: '#4fc3f7', fontWeight: 'bold', fontFamily: 'monospace', fontSize: 11 }}
    >
      {label}
    </Typography>
    <Box sx={{ ml: 1, color: 'grey.300', fontFamily: 'monospace', fontSize: 11 }}>
      {children}
    </Box>
  </Box>
);

const Line: FC<{ label: string; value: string | number }> = ({ label, value }) => (
  <Box>
    <span style={{ color: '#aaa' }}>{label}: </span>
    <span style={{ color: '#e0e0e0' }}>{String(value)}</span>
  </Box>
);

const Row: FC<{ children: React.ReactNode }> = ({ children }) => (
  <Box sx={{ fontFamily: 'monospace', fontSize: 11, color: '#e0e0e0' }}>{children}</Box>
);

export default DebugPanel;
