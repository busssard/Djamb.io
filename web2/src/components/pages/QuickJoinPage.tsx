import React, { FC } from 'react';
import { Typography } from '@mui/material';
import RedirectToHomeIfSignedIn from '../routing/RedirectToHomeIfSignedIn';
import QuickJoinForm from '../forms/QuickJoinForm';

const QuickJoinPage: FC = () => {
  return (
    <div>
      <RedirectToHomeIfSignedIn />
      <Typography variant="h4">Join</Typography>
      <Typography variant="body2" sx={{ mt: 1, mb: 2, color: 'text.secondary' }}>
        Pick a username to start playing. No password needed.
      </Typography>
      <QuickJoinForm />
    </div>
  );
};

export default QuickJoinPage;
