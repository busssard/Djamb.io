import React, { FC, useEffect, useState } from 'react';
import {
  TableCell,
  TableRow,
  TextField,
  Button,
  Select,
  MenuItem,
  Stack,
  FormControl,
} from '@mui/material';
import { Add as AddIcon, Remove as RemoveIcon } from '@mui/icons-material';
import { useSelector } from 'react-redux';
import { GameDto, PlayerKind } from '../../../api-client';
import { LobbyPlayerViewModel, LobbyPlayerActionType } from './viewModel';
import {
  addPlayer,
  removePlayer,
  addBotPlayer,
  fetchBots,
  BotInfo,
} from '../../../controllers/gameController';
import { selectSession } from '../../../hooks/selectors';
import { formStyles } from '../../../styles/styles';

interface Props {
  player: LobbyPlayerViewModel;
  game: GameDto;
}

// Cache bots across all row instances
let cachedBots: BotInfo[] | null = null;

const LobbyPlayerRow: FC<Props> = ({ player, game }) => {
  const [guestName, setGuestName] = useState('');
  const [bots, setBots] = useState<BotInfo[]>(cachedBots || []);
  const [selectedBot, setSelectedBot] = useState('');
  const { user } = useSelector(selectSession);

  const needsBots =
    player.actionType === LobbyPlayerActionType.AddBot ||
    player.actionType === LobbyPlayerActionType.AddGuest;

  useEffect(() => {
    if (needsBots && !cachedBots) {
      fetchBots().then((result) => {
        cachedBots = result;
        setBots(result);
        if (result.length > 0 && !selectedBot) {
          setSelectedBot(result[0].name);
        }
      });
    } else if (needsBots && cachedBots && cachedBots.length > 0 && !selectedBot) {
      setSelectedBot(cachedBots[0].name);
    }
  }, [needsBots, selectedBot]);

  if (user === null) {
    return <></>;
  }

  const isDuplicateName = guestName
    ? game.players.find((p) => p.name === guestName)
    : undefined;

  const helperText = isDuplicateName
    ? `There is already a player named ${guestName}`
    : '';

  const isValidName = !isDuplicateName;

  const addGuest = () => {
    addPlayer(game.id, {
      userId: user.id,
      name: guestName,
      kind: PlayerKind.Guest,
    });
  };

  const addBot = () => {
    if (!selectedBot) return;
    addBotPlayer(game.id, selectedBot);
  };

  const selfJoin = () => {
    addPlayer(game.id, {
      userId: user.id,
      kind: PlayerKind.User,
    });
  };

  const remove = () => {
    if (player.id === null) {
      throw new Error('Cannot remove player without ID.');
    }
    removePlayer(game.id, player.id);
  };

  // Bot picker row (remaining empty slots)
  if (player.actionType === LobbyPlayerActionType.AddBot) {
    return (
      <TableRow>
        <TableCell>
          <FormControl size="small">
            <Select
              value={selectedBot}
              onChange={(e) => setSelectedBot(e.target.value)}
              displayEmpty
              style={{ minWidth: 120 }}
            >
              {bots.map((b) => (
                <MenuItem key={b.name} value={b.name}>
                  {b.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </TableCell>
        <TableCell>
          {bots.find((b) => b.name === selectedBot)?.description || ''}
        </TableCell>
        <TableCell>
          <Button
            sx={formStyles.button}
            onClick={addBot}
            style={{ width: '100%' }}
            disabled={!selectedBot}
          >
            <AddIcon style={{ paddingRight: '5px' }} />
            Add bot
          </Button>
        </TableCell>
      </TableRow>
    );
  }

  // AddGuest row: guest name input + guest button, and bot dropdown + bot button
  if (player.actionType === LobbyPlayerActionType.AddGuest) {
    return (
      <TableRow>
        <TableCell>
          <TextField
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            error={!isValidName}
            helperText={helperText}
            placeholder="Guest player name"
          />
        </TableCell>
        <TableCell></TableCell>
        <TableCell>
          <Stack spacing={1}>
            <Button
              sx={formStyles.button}
              onClick={addGuest}
              style={{ width: '100%' }}
              disabled={!guestName || !isValidName}
            >
              <AddIcon style={{ paddingRight: '5px' }} />
              Add guest
            </Button>
            {bots.length > 0 && (
              <Stack direction="row" spacing={1} alignItems="center">
                <FormControl size="small">
                  <Select
                    value={selectedBot}
                    onChange={(e) => setSelectedBot(e.target.value)}
                    style={{ minWidth: 90 }}
                  >
                    {bots.map((b) => (
                      <MenuItem key={b.name} value={b.name}>
                        {b.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <Button sx={formStyles.button} onClick={addBot} disabled={!selectedBot}>
                  <AddIcon style={{ paddingRight: '5px' }} />
                  Bot
                </Button>
              </Stack>
            )}
          </Stack>
        </TableCell>
      </TableRow>
    );
  }

  // Standard row for existing players, self-join, quit, remove
  const onClick = () => {
    switch (player.actionType) {
      case LobbyPlayerActionType.SelfJoin:
        selfJoin();
        break;
      case LobbyPlayerActionType.SelfQuit:
      case LobbyPlayerActionType.Remove:
        remove();
        break;
      default:
        break;
    }
  };

  const getActionIcon = (action: LobbyPlayerActionType | null) => {
    switch (action) {
      case LobbyPlayerActionType.SelfJoin:
        return <AddIcon style={{ paddingRight: '5px' }} />;
      case LobbyPlayerActionType.Remove:
      case LobbyPlayerActionType.SelfQuit:
        return <RemoveIcon style={{ paddingRight: '5px' }} />;
      default:
        return <></>;
    }
  };

  const getActionLabel = (action: LobbyPlayerActionType | null) => {
    switch (action) {
      case LobbyPlayerActionType.SelfJoin:
        return 'Join';
      case LobbyPlayerActionType.SelfQuit:
        return 'Quit';
      case LobbyPlayerActionType.Remove:
        return 'Remove';
      default:
        return '';
    }
  };

  return (
    <TableRow>
      <TableCell>{player.name}</TableCell>
      <TableCell>{player.note}</TableCell>
      <TableCell>
        {player.actionType === LobbyPlayerActionType.None || player.actionType === null ? (
          <></>
        ) : (
          <Button sx={formStyles.button} onClick={onClick} style={{ width: '100%' }}>
            {getActionIcon(player.actionType)}
            {getActionLabel(player.actionType)}
          </Button>
        )}
      </TableCell>
    </TableRow>
  );
};

export default LobbyPlayerRow;
