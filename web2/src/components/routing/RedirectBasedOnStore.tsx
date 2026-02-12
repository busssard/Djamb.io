import { useSelector } from 'react-redux';
import React, { FC } from 'react';
import { useLocation, Navigate } from 'react-router-dom';
import { selectNavigation } from '../../hooks/selectors';
import { defaultNavigationState } from '../../redux/navigation/state';

const RedirectBasedOnStore: FC = () => {
  const navigation = useSelector(selectNavigation);
  const location = useLocation();

  if (navigation !== defaultNavigationState && navigation.path !== location.pathname) {
    return <Navigate to={navigation.path} replace />;
  }

  return <></>;
};

export default RedirectBasedOnStore;
