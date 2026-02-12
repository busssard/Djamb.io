import React, { FC } from 'react';
import { IconButton, Tooltip } from '@mui/material';
import GetAppIcon from '@mui/icons-material/GetApp';
import { useInstallPrompt } from '../../hooks/useInstallPrompt';

const InstallButton: FC = () => {
  const { canInstall, promptInstall } = useInstallPrompt();

  if (!canInstall) return null;

  return (
    <Tooltip title="Install app">
      <IconButton color="inherit" onClick={promptInstall}>
        <GetAppIcon />
      </IconButton>
    </Tooltip>
  );
};

export default InstallButton;
