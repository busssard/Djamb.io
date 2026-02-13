import React, { FC, ChangeEvent } from 'react';
import { FormControlLabel, TextField } from '@mui/material';
import { formStyles } from '../../../styles/styles';

interface Props {
  value: string;
  label: string;
  onChanged: (e: ChangeEvent<HTMLInputElement>) => void;
  error?: boolean;
  helperText?: string;
}

const FormPasswordField: FC<Props> = ({ value, label, onChanged, error, helperText }) => {
  return (
    <FormControlLabel
      value={value}
      label={label}
      labelPlacement="start"
      sx={formStyles.label}
      control={
        <TextField
          sx={formStyles.control}
          onChange={onChanged}
          error={error}
          helperText={helperText}
          type="password"
        />
      }
    />
  );
};

export default FormPasswordField;
