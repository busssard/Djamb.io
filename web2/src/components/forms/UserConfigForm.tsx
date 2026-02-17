import React, { FC, useState } from 'react';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  FormControl,
  FormGroup,
  TableBody,
  Table,
  TableRow,
  Checkbox,
  TextField,
  Typography,
} from '@mui/material';
import { useSelector } from 'react-redux';
import { setUserConfig } from '../../controllers/configController';
import FormSubmitButton from './controls/FormSubmitButton';
import { selectConfig } from '../../hooks/selectors';
import { formStyles } from '../../styles/styles';
import FormTableCell from './controls/FormTableCell';
import { pieceSkins } from '../../model/pieceSkins';

const TableCell = FormTableCell;

const previewPieces = ['hunter', 'diplomat', 'reaper', 'thug'];

const UserConfigForm: FC = () => {
  const { user } = useSelector(selectConfig);
  const [state, setState] = useState(user);
  const submit = () => setUserConfig(state);

  return (
    <FormControl component="fieldset" onSubmit={submit}>
      <FormGroup>
        <Typography variant="subtitle2" sx={{ mb: 1, color: 'text.secondary' }}>
          Piece Style
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
          {pieceSkins.map((skin) => (
            <Card
              key={skin.id}
              variant="outlined"
              sx={{
                width: 140,
                border: state.pieceSkin === skin.id ? '2px solid' : '1px solid',
                borderColor: state.pieceSkin === skin.id ? 'primary.main' : 'divider',
              }}
            >
              <CardActionArea
                onClick={() => setState({ ...state, pieceSkin: skin.id })}
              >
                <CardContent sx={{ p: 1, '&:last-child': { pb: 1 } }}>
                  <Typography variant="body2" align="center" sx={{ mb: 0.5 }}>
                    {skin.name}
                  </Typography>
                  <Box sx={{ display: 'flex', justifyContent: 'center', gap: 0.5 }}>
                    {previewPieces.map((piece) => (
                      <img
                        key={piece}
                        src={`${skin.path}/${piece}.png`}
                        alt={piece}
                        style={{ width: 28, height: 28, objectFit: 'contain' }}
                      />
                    ))}
                  </Box>
                </CardContent>
              </CardActionArea>
            </Card>
          ))}
        </Box>

        <Table>
          <TableBody>
            <TableRow>
              <TableCell>Log Redux</TableCell>
              <TableCell>
                <Checkbox
                  sx={formStyles.control}
                  checked={state.logRedux}
                  onChange={(e) =>
                    setState({
                      ...state,
                      logRedux: e.target.checked,
                    })
                  }
                />
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Show cell and piece IDs</TableCell>
              <TableCell>
                <Checkbox
                  sx={formStyles.control}
                  checked={state.showCellAndPieceIds}
                  onChange={(e) =>
                    setState({
                      ...state,
                      showCellAndPieceIds: e.target.checked,
                    })
                  }
                />
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Show board tooltips</TableCell>
              <TableCell>
                <Checkbox
                  sx={formStyles.control}
                  checked={state.showBoardTooltips}
                  onChange={(e) =>
                    setState({
                      ...state,
                      showBoardTooltips: e.target.checked,
                    })
                  }
                />
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell>Seconds to display notifications</TableCell>
              <TableCell>
                <TextField
                  type="number"
                  sx={formStyles.control}
                  value={state.notificationDisplaySeconds}
                  onChange={(e) =>
                    setState({
                      ...state,
                      notificationDisplaySeconds: Number(e.target.value),
                    })
                  }
                />
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
        <br />
        <FormSubmitButton text="Save" onClick={submit} />
      </FormGroup>
    </FormControl>
  );
};

export default UserConfigForm;
