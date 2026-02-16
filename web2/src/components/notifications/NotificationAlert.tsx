import React from 'react';
import { Alert } from '@mui/material';
import { Notification, NotificationLevel } from '../../model/notifications';
import { removeNotification } from '../../controllers/notificationsController';

type AlertSeverity = 'info' | 'success' | 'warning' | 'error' | undefined;

function getAlertSeverity(level: NotificationLevel): AlertSeverity {
  switch (level) {
    case NotificationLevel.Info:
      return 'info';
    case NotificationLevel.Warning:
      return 'warning';
    case NotificationLevel.Error:
      return 'error';
    default:
      return undefined;
  }
}

interface AlertProps {
  notification: Notification;
}

const NotificationAlert = React.forwardRef<HTMLDivElement, AlertProps>(({ notification }, ref) => {
  const messages = notification.message.split('\n');

  return (
    <Alert
      ref={ref}
      severity={getAlertSeverity(notification.level)}
      onClose={() => removeNotification(notification.id)}
    >
      {messages.map((m, i) => (
        <div key={i.toString()} style={{ textAlign: 'left' }}>
          {m}
        </div>
      ))}
    </Alert>
  );
});

NotificationAlert.displayName = 'NotificationAlert';

export default NotificationAlert;
