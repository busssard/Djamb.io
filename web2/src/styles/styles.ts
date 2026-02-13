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

const playerColors = new Map<number, string>([
  [0, 'blue'],
  [1, 'red'],
  [2, 'green'],
  [3, 'orange'],
  [4, 'brown'],
  [5, 'teal'],
  [6, 'magenta'],
  [7, 'gold'],
]);

export const pieceColors = {
  placeholder: '#CCCC33',
  getPlayer: (colorId: number | null): string => {
    if (colorId === null) {
      return '#555555';
    }
    const color = playerColors.get(colorId);
    if (!color) {
      throw Error(`Invalid colorID: ${colorId}`);
    }
    return color;
  },
};
