import { SxProps, Theme } from '@mui/material/styles';

export const formStyles = {
  button: {
    border: '1px solid',
    padding: '10px',
    width: '50%',
    alignSelf: 'center',
  } as SxProps<Theme>,
  control: {
    padding: '10px',
  } as SxProps<Theme>,
  label: {
    color: 'text.secondary',
  } as SxProps<Theme>,
};

// 8 maximally distinct, saturated player colors.
// These must be visually distinguishable from each other at small sizes
// on a dark game board. Order doesn't matter — the backend assigns
// random colorIds from 0-7 regardless of player count.
const playerColors = new Map<number, string>([
  [0, '#E03030'], // Red
  [1, '#3080E0'], // Blue
  [2, '#30B030'], // Green
  [3, '#E0C020'], // Yellow
  [4, '#E07020'], // Orange
  [5, '#A040D0'], // Purple
  [6, '#20C0C0'], // Cyan
  [7, '#E050A0'], // Pink
]);

export const pieceColors = {
  // The new piece icons use pure red (#FF0000) as the team color placeholder.
  // The replaceColor function swaps this with the actual player color.
  placeholder: '#FF0000',
  neutral: '#666666',
  getPlayer: (colorId: number | null): string => {
    if (colorId === null) {
      return pieceColors.neutral;
    }
    const color = playerColors.get(colorId);
    if (!color) {
      throw Error(`Invalid colorID: ${colorId}`);
    }
    return color;
  },
};
