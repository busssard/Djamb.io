import React, { FC, useState } from 'react';
import {
  Box,
  FormControl,
  FormGroup,
  MenuItem,
  Select,
  Table,
  TableBody,
  TableRow,
  TextField,
  Checkbox,
  Typography,
} from '@mui/material';
import { createGame } from '../../controllers/gameController';
import FormSubmitButton from './controls/FormSubmitButton';
import FormTableCell from './controls/FormTableCell';

type FormState = {
  description: string;
  allowGuests: boolean;
  isPublic: boolean;
  playerCount: number;
  rulesetKind: number;
  turnTimeLimitSeconds: number | null;
};

const defaultState: FormState = {
  description: '',
  allowGuests: true,
  isPublic: true,
  playerCount: 3,
  rulesetKind: 1,
  turnTimeLimitSeconds: null,
};

const TableCell = FormTableCell;

const CreateGameForm: FC = () => {
  const [state, setState] = useState(defaultState);

  const submit = () => {
    const regionCount = state.playerCount === 2 ? 4 : state.playerCount;
    createGame({
      description: state.description,
      allowGuests: state.allowGuests,
      isPublic: state.isPublic,
      regionCount,
      rulesetKind: state.rulesetKind,
      turnTimeLimitSeconds: state.turnTimeLimitSeconds,
    });
  };

  return (
    <div>
      <FormControl component="fieldset" onSubmit={submit}>
        <FormGroup>
          <Table>
            <TableBody>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>
                  <TextField
                    value={state.description}
                    onChange={(e) =>
                      setState({
                        ...state,
                        description: e.target.value,
                      })
                    }
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        submit();
                      }
                    }}
                  />
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>
                  <Box>
                    <div>Allow guest players</div>
                    <Typography variant="caption" color="text.secondary">
                      Allow two players to play from the same device
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Checkbox
                    checked={state.allowGuests}
                    onChange={(e) =>
                      setState({
                        ...state,
                        allowGuests: e.target.checked,
                      })
                    }
                  />
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Public</TableCell>
                <TableCell>
                  <Checkbox
                    checked={state.isPublic}
                    onChange={(e) =>
                      setState({
                        ...state,
                        isPublic: e.target.checked,
                      })
                    }
                  />
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Ruleset</TableCell>
                <TableCell>
                  <Select
                    value={state.rulesetKind}
                    onChange={(e) =>
                      setState({
                        ...state,
                        rulesetKind: Number(e.target.value),
                      })
                    }
                  >
                    <MenuItem value={1}>Total War</MenuItem>
                    <MenuItem value={2}>Classic</MenuItem>
                  </Select>
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Players</TableCell>
                <TableCell>
                  <Select
                    value={state.playerCount}
                    onChange={(e) =>
                      setState({
                        ...state,
                        playerCount: Number(e.target.value),
                      })
                    }
                  >
                    {[2, 3, 4, 5, 6, 7, 8].map((n) => (
                      <MenuItem key={n} value={n}>
                        {n}
                      </MenuItem>
                    ))}
                  </Select>
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Turn time limit</TableCell>
                <TableCell>
                  <Select
                    value={state.turnTimeLimitSeconds ?? 0}
                    onChange={(e) =>
                      setState({
                        ...state,
                        turnTimeLimitSeconds: Number(e.target.value) || null,
                      })
                    }
                  >
                    <MenuItem value={0}>No limit</MenuItem>
                    <MenuItem value={300}>5 minutes</MenuItem>
                    <MenuItem value={600}>10 minutes</MenuItem>
                    <MenuItem value={1800}>30 minutes</MenuItem>
                    <MenuItem value={3600}>1 hour</MenuItem>
                    <MenuItem value={28800}>8 hours</MenuItem>
                    <MenuItem value={86400}>24 hours</MenuItem>
                    <MenuItem value={604800}>1 week</MenuItem>
                  </Select>
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
          <br />
          <FormSubmitButton text="Submit" onClick={submit} />
        </FormGroup>
      </FormControl>
    </div>
  );
};

export default CreateGameForm;
