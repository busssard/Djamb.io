import React, { FC } from 'react';
import { AppBar, Toolbar, Typography, Grid } from '@mui/material';
import { Theme } from '@mui/material/styles';
import { makeStyles } from '@mui/styles';
import MenuButton from './MenuButton';
import NotificationsButton from './NotificationsButton';
import InstallButton from './InstallButton';

const useStyles = makeStyles((theme: Theme) => ({
  root: {
    flexGrow: 1,
  },
  title: {
    flexGrow: 1,
    color: theme.palette.text.secondary,
    position: 'absolute',
    transform: 'translate(-50%, -50%)',
    margin: 0,
    top: '50%',
    left: '50%',
  },
}));

const TopBar: FC = () => {
  const classes = useStyles();

  return (
    <div className={classes.root}>
      <AppBar position="static" color="inherit">
        <Toolbar>
          <Grid container className={classes.root}>
            <Grid size="grow" style={{ display: 'flex' }}>
              <MenuButton />
              <NotificationsButton />
            </Grid>
            <Grid size="grow">
              <Typography variant="h6" className={classes.title}>
                Djambi-N
              </Typography>
            </Grid>
            <Grid size="grow" style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <InstallButton />
            </Grid>
          </Grid>
        </Toolbar>
      </AppBar>
    </div>
  );
};

export default TopBar;
