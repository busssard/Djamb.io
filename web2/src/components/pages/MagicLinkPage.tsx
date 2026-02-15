import React, { FC } from 'react';
import { Typography } from '@mui/material';
import RedirectToHomeIfSignedIn from '../routing/RedirectToHomeIfSignedIn';
import RequestMagicLinkForm from '../forms/RequestMagicLinkForm';

const MagicLinkPage: FC = () => {
  return (
    <div>
      <RedirectToHomeIfSignedIn />
      <Typography variant="h4">Sign in on this device</Typography>
      <Typography variant="body2" sx={{ mt: 1, mb: 2, color: 'text.secondary' }}>
        Enter the email you used when you joined. We will send you a link to sign in.
      </Typography>
      <RequestMagicLinkForm />
    </div>
  );
};

export default MagicLinkPage;
