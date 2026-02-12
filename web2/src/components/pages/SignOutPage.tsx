import React, { FC } from 'react';
import { Typography } from '@mui/material';
import RedirectToSignInIfSignedOut from '../routing/RedirectToSignInIfSignedOut';

const SignOutPage: FC = () => {
  return (
    <div>
      <RedirectToSignInIfSignedOut />
      <Typography variant="h4">
        Sign out
      </Typography>
    </div>
  );
};

export default SignOutPage;
