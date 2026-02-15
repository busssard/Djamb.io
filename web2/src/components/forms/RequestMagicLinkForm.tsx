import React, { FC, useState } from 'react';
import { FormControl, FormGroup, Typography } from '@mui/material';
import FormTextField from './controls/FormTextField';
import FormSubmitButton from './controls/FormSubmitButton';
import { store } from '../../redux';

const RequestMagicLinkForm: FC = () => {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    setError(null);
    const state = store.getState();
    const apiUrl = state.config.environment.apiUrl;

    try {
      const response = await fetch(`${apiUrl}/api/sessions/magic-link`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email }),
      });

      if (!response.ok) {
        setError('Something went wrong. Please try again.');
        return;
      }

      setSent(true);
    } catch {
      setError('Network error. Please try again.');
    }
  };

  if (sent) {
    return (
      <div>
        <Typography variant="body1" sx={{ mt: 2 }}>
          If an account exists with that email, a sign-in link has been sent. Check your inbox.
        </Typography>
      </div>
    );
  }

  return (
    <div>
      <FormControl component="fieldset" onSubmit={submit}>
        <FormGroup>
          <FormTextField
            label="Email"
            value={email}
            onChanged={(e) => setEmail(e.target.value)}
          />
          {error && (
            <Typography variant="body2" color="error" sx={{ mt: 1 }}>
              {error}
            </Typography>
          )}
          <br />
          <FormSubmitButton text="Send sign-in link" onClick={submit} />
        </FormGroup>
      </FormControl>
    </div>
  );
};

export default RequestMagicLinkForm;
