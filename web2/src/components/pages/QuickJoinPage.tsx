import React, { FC } from 'react';
import { Typography, Link } from '@mui/material';
import RedirectToHomeIfSignedIn from '../routing/RedirectToHomeIfSignedIn';
import QuickJoinForm from '../forms/QuickJoinForm';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const QuickJoinPage: FC = () => {
  return (
    <div>
      <RedirectToHomeIfSignedIn />
      <Typography variant="h4">Join</Typography>
      <Typography variant="body2" sx={{ mt: 1, mb: 2, color: 'text.secondary' }}>
        Pick a username to start playing. No password needed.
      </Typography>
      <QuickJoinForm />
      <Typography variant="body2" sx={{ mt: 3, color: 'text.secondary' }}>
        Already have an account?{' '}
        <Link
          component="button"
          variant="body2"
          onClick={() => navigateTo(Routes.magicLink)}
        >
          Sign in on this device
        </Link>
      </Typography>
    </div>
  );
};

export default QuickJoinPage;
