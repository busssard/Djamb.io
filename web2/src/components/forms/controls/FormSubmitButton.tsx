import React, { FC } from 'react';
import { Button } from '@mui/material';
import { formStyles } from '../../../styles/styles';

interface FormSubmitButtonProps {
  onClick: () => void;
  text: string;
}

const FormSubmitButton: FC<FormSubmitButtonProps> = ({ onClick, text }) => {
  return (
    <Button onClick={onClick} sx={formStyles.button} type="submit">
      {text}
    </Button>
  );
};

export default FormSubmitButton;
