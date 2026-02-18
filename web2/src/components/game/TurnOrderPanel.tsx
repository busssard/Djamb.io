import React, { FC } from 'react';
import { Box, Typography } from '@mui/material';
import { GameDto, PlayerStatus } from '../../api-client';
import { pieceColors } from '../../styles/styles';

interface Props {
  game: GameDto;
}

const TurnOrderPanel: FC<Props> = ({ game }) => {
  const turnCycle = game.turnCycle ?? [];
  if (turnCycle.length === 0) return null;

  const currentPlayerId = turnCycle[0];

  // Build the ordered list: turnCycle already has current player first
  const orderedPlayers = turnCycle
    .map((pid) => game.players?.find((p) => p.id === pid))
    .filter(Boolean) as NonNullable<(typeof game.players)[number]>[];

  return (
    <Box
      sx={{
        position: 'absolute',
        top: 8,
        left: 8,
        display: 'flex',
        flexDirection: 'column',
        gap: 0.5,
        bgcolor: 'rgba(0, 0, 0, 0.6)',
        borderRadius: 1,
        px: 1.5,
        py: 1,
        pointerEvents: 'none',
        zIndex: 10,
      }}
    >
      {orderedPlayers.map((player) => {
        const color = pieceColors.getPlayer(player.colorId ?? null);
        const isCurrent = player.id === currentPlayerId;
        const isEliminated =
          player.status === PlayerStatus.Eliminated || player.status === PlayerStatus.Conceded;

        return (
          <Box
            key={player.id}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              opacity: isEliminated ? 0.4 : 1,
            }}
          >
            <Box
              sx={{
                width: 12,
                height: 12,
                borderRadius: '50%',
                bgcolor: color,
                flexShrink: 0,
                boxShadow: isCurrent ? `0 0 6px 2px ${color}` : 'none',
              }}
            />
            <Typography
              sx={{
                color: isCurrent ? '#fff' : 'rgba(255,255,255,0.7)',
                fontSize: 13,
                fontWeight: isCurrent ? 700 : 400,
                lineHeight: 1.3,
                textDecoration: isEliminated ? 'line-through' : 'none',
                whiteSpace: 'nowrap',
              }}
            >
              {player.name}
            </Typography>
          </Box>
        );
      })}
    </Box>
  );
};

export default TurnOrderPanel;
