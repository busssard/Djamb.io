import React, { FC } from 'react';
import { Typography } from '@mui/material';
import SignInForm from '../forms/SignInForm';
import RedirectToHomeIfSignedIn from '../routing/RedirectToHomeIfSignedIn';

const SignInPage: FC = () => {
  return (
    <div>
      <RedirectToHomeIfSignedIn />
      <Typography variant="h4">Sign in</Typography>
      <SignInForm />
    </div>
  );
};

export default SignInPage;
