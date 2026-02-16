import React, { FC } from 'react';
import { Table, TableContainer, TableCell, TableRow, TableBody, TableHead, Chip } from '@mui/material';
import { SearchGameDto, GameStatus } from '../../api-client';
import * as Routes from '../../utilities/routes';
import { navigateTo } from '../../controllers/navigationController';

interface Props {
  games: SearchGameDto[];
}

const rowSx = {
  cursor: 'pointer',
  background: '#000000',
  '&:hover': {
    background: '#555555',
  },
} as const;

function statusLabel(status: GameStatus): string {
  switch (status) {
    case GameStatus.Pending:
      return 'Open';
    case GameStatus.InProgress:
      return 'In Progress';
    case GameStatus.Canceled:
      return 'Canceled';
    case GameStatus.Over:
      return 'Finished';
    default:
      return String(status);
  }
}

function statusColor(status: GameStatus): 'success' | 'info' | 'default' | 'warning' {
  switch (status) {
    case GameStatus.Pending:
      return 'success';
    case GameStatus.InProgress:
      return 'info';
    default:
      return 'default';
  }
}

const GameSearchResultsTable: FC<Props> = ({ games }) => {
  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Description</TableCell>
            <TableCell>Players</TableCell>
            <TableCell>Status</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {games.map((g) => (
            <TableRow
              sx={rowSx}
              key={g.id}
              onClick={() => navigateTo(Routes.game(g.id))}
            >
              <TableCell>{g.id}</TableCell>
              <TableCell>{g.parameters.description || '(no description)'}</TableCell>
              <TableCell>
                {g.playerCount}/{g.parameters.regionCount}
              </TableCell>
              <TableCell>
                <Chip label={statusLabel(g.status)} color={statusColor(g.status)} size="small" />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default GameSearchResultsTable;
