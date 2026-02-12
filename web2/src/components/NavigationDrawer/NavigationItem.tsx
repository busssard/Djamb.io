import React, { FC } from 'react';
import { ListItemButton, ListItemIcon, ListItemText } from '@mui/material';
import { navigateTo } from '../../controllers/navigationController';

interface NavigationItemProps {
  text: string;
  icon: JSX.Element;
  path: string;
}

const NavigationItem: FC<NavigationItemProps> = ({ text, icon, path }) => {
  const onClick = () => {
    navigateTo(path);
  };

  return (
    <ListItemButton key={text} onClick={onClick}>
      <ListItemIcon>{icon}</ListItemIcon>
      <ListItemText primary={text} />
    </ListItemButton>
  );
};

export default NavigationItem;
