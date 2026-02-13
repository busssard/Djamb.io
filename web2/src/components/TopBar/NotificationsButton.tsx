import React, { FC } from 'react';
import { IconButton, Typography } from '@mui/material';
import { Notifications as NotificationsIcon } from '@mui/icons-material';
import { useSelector } from 'react-redux';
import { selectNotifications } from '../../hooks/selectors';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';
import { topBarButtonSx } from './styles';

const NotificationsButton: FC = () => {
  const notifications = useSelector(selectNotifications).notifications;
  const count = notifications.length;
  const disabled = count === 0;

  return (
    <IconButton
      edge="start"
      sx={topBarButtonSx}
      onClick={() => navigateTo(Routes.notifications)}
      disabled={disabled}
    >
      <>
        <NotificationsIcon />
        {disabled ? <></> : <Typography>{count}</Typography>}
      </>
    </IconButton>
  );
};

export default NotificationsButton;
