import React, { FC, ChangeEvent, KeyboardEvent } from 'react';
import { FormControlLabel, TextField } from '@mui/material';
import { formStyles } from '../../../styles/styles';

interface Props {
  value: string;
  label: string;
  onChanged: (e: ChangeEvent<HTMLInputElement>) => void;
  onSubmit?: () => void;
  error?: boolean;
  helperText?: string;
  placeholder?: string;
}

const FormTextField: FC<Props> = ({ value, label, onChanged, onSubmit, error, helperText, placeholder }) => {
  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && onSubmit) {
      e.preventDefault();
      onSubmit();
    }
  };

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
          onKeyDown={handleKeyDown}
          error={error}
          helperText={helperText}
          placeholder={placeholder}
        />
      }
    />
  );
};

export default FormTextField;
