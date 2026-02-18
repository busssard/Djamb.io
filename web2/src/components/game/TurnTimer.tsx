import React, { FC, useEffect, useState } from 'react';
import { Typography } from '@mui/material';

type TurnTimerProps = {
  turnStartedAt: string;
  turnTimeLimitSeconds: number;
};

function formatRemaining(totalSeconds: number): string {
  if (totalSeconds <= 0) return '0:00';

  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  if (days > 0) {
    return `${days}d ${hours}h`;
  }
  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  }
  return `${minutes}:${String(seconds).padStart(2, '0')}`;
}

const TurnTimer: FC<TurnTimerProps> = ({ turnStartedAt, turnTimeLimitSeconds }) => {
  const [remaining, setRemaining] = useState<number>(() => {
    const elapsed = (Date.now() - new Date(turnStartedAt).getTime()) / 1000;
    return Math.max(0, Math.floor(turnTimeLimitSeconds - elapsed));
  });

  useEffect(() => {
    const update = () => {
      const elapsed = (Date.now() - new Date(turnStartedAt).getTime()) / 1000;
      setRemaining(Math.max(0, Math.floor(turnTimeLimitSeconds - elapsed)));
    };

    update();
    const interval = setInterval(update, 1000);
    return () => clearInterval(interval);
  }, [turnStartedAt, turnTimeLimitSeconds]);

  const isUrgent = remaining <= 60;

  return (
    <Typography
      variant="h6"
      sx={{
        textAlign: 'center',
        py: 0.5,
        color: isUrgent ? '#ff4444' : '#cccccc',
        fontFamily: 'monospace',
        fontWeight: isUrgent ? 'bold' : 'normal',
      }}
    >
      {formatRemaining(remaining)}
    </Typography>
  );
};

export default TurnTimer;
