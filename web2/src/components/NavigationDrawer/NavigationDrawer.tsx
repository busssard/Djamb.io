import React, { FC } from 'react';
import { Box, Drawer } from '@mui/material';
import { useSelector } from 'react-redux';
import GamelessSection from './GamelessSection/GamelessSection';
import ActiveGameSection from './ActiveGameSection/ActiveGameSection';
import { selectNavigation } from '../../hooks/selectors';
import { toggleDrawer } from '../../controllers/navigationController';

const NavigationDrawer: FC = () => {
  const state = useSelector(selectNavigation);
  const isOpen = state.isDrawerOpen;

  const close = (event: React.MouseEvent | React.KeyboardEvent) => {
    if (event.type === 'keydown') {
      const e = event as React.KeyboardEvent;
      if (e.key === 'Tab' || e.key === 'Shift') {
        return;
      }
    }

    toggleDrawer(false);
  };

  return (
    <div>
      <Drawer open={isOpen} onClose={close}>
        <Box
          sx={{ width: 250 }}
          role="presentation"
          onClick={close}
          onKeyDown={close}
        >
          <GamelessSection />
          <ActiveGameSection />
        </Box>
      </Drawer>
    </div>
  );
};

export default NavigationDrawer;
