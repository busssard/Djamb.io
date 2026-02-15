import React, { FC, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Typography, CircularProgress, Box } from '@mui/material';
import { loggedIn } from '../../redux/session/actionFactory';
import { store } from '../../redux';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const MagicLinkVerifyPage: FC = () => {
  const { token } = useParams<{ token: string }>();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) {
      setError('Invalid link.');
      return;
    }

    const state = store.getState();
    const apiUrl = state.config.environment.apiUrl;

    fetch(`${apiUrl}/api/sessions/magic-link/verify`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ token }),
    })
      .then(async (response) => {
        if (!response.ok) {
          const text = await response.text();
          let message = 'Verification failed.';
          try {
            const problem = JSON.parse(text);
            if (problem.title) message = problem.title;
          } catch {
            // use default
          }
          setError(message);
          return;
        }

        const session = await response.json();
        const action = loggedIn(session.user);
        store.dispatch(action);
        navigateTo(Routes.home);
      })
      .catch(() => {
        setError('Network error. Please try again.');
      });
  }, [token]);

  if (error) {
    return (
      <div>
        <Typography variant="h4">Sign-in failed</Typography>
        <Typography variant="body1" sx={{ mt: 2 }}>
          {error}
        </Typography>
      </div>
    );
  }

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
      <CircularProgress />
      <Typography variant="body1" sx={{ ml: 2 }}>
        Verifying...
      </Typography>
    </Box>
  );
};

export default MagicLinkVerifyPage;
