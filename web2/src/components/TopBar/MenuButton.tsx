import React, { FC } from 'react';
import { IconButton } from '@mui/material';
import { Menu as MenuIcon } from '@mui/icons-material';
import { useSelector } from 'react-redux';
import { selectNavigation } from '../../hooks/selectors';
import { toggleDrawer } from '../../controllers/navigationController';
import { topBarButtonSx } from './styles';

const MenuButton: FC = () => {
  const state = useSelector(selectNavigation);
  const isOpen = state.isDrawerOpen;

  const toggle = () => {
    toggleDrawer(!isOpen);
  };

  return (
    <IconButton edge="start" sx={topBarButtonSx} onClick={toggle}>
      <MenuIcon />
    </IconButton>
  );
};

export default MenuButton;
