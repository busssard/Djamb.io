import React, { FC } from 'react';
import { Typography, Link } from '@mui/material';

const RulesPage: FC = () => {
  return (
    <div>
      <Typography variant="h4">Rules</Typography>
      <br />
      <Typography variant="body1">
        {'The rules are not currently embedded in the app, but can be found on the '}
        <Link href="https://en.wikipedia.org/wiki/Djambi">Wikipedia article</Link>.
      </Typography>
    </div>
  );
};

export default RulesPage;
