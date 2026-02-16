import React, { FC } from 'react';
import { AppBar, Toolbar, Typography, Grid, Box } from '@mui/material';
import MenuButton from './MenuButton';
import NotificationsButton from './NotificationsButton';
import InstallButton from './InstallButton';

const rootSx = { flexGrow: 1 } as const;

const titleSx = {
  flexGrow: 1,
  color: 'text.secondary',
  position: 'absolute',
  transform: 'translate(-50%, -50%)',
  margin: 0,
  top: '50%',
  left: '50%',
} as const;

const TopBar: FC = () => {
  return (
    <Box sx={rootSx}>
      <AppBar position="static" color="inherit">
        <Toolbar>
          <Grid container sx={rootSx}>
            <Grid size="grow" style={{ display: 'flex' }}>
              <MenuButton />
              <NotificationsButton />
            </Grid>
            <Grid size="grow">
              <Typography variant="h6" sx={titleSx}>
                Djamb.io
              </Typography>
            </Grid>
            <Grid size="grow" style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <InstallButton />
            </Grid>
          </Grid>
        </Toolbar>
      </AppBar>
    </Box>
  );
};

export default TopBar;
