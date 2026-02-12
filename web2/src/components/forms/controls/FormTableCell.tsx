import { TableCell } from '@mui/material';
import { withStyles } from '@mui/styles';

const FormTableCell = withStyles({
  root: {
    borderBottom: 'none',
  },
})(TableCell);

export default FormTableCell;
