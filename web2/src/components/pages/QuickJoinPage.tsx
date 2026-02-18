import React, { FC } from 'react';
import { Typography, Link, Box, Container, Paper } from '@mui/material';
import GroupsIcon from '@mui/icons-material/Groups';
import ExtensionIcon from '@mui/icons-material/Extension';
import VisibilityIcon from '@mui/icons-material/Visibility';
import LinkIcon from '@mui/icons-material/Link';
import RedirectToHomeIfSignedIn from '../routing/RedirectToHomeIfSignedIn';
import QuickJoinForm from '../forms/QuickJoinForm';
import { navigateTo } from '../../controllers/navigationController';
import * as Routes from '../../utilities/routes';

const FeatureItem: FC<{ icon: React.ReactNode; title: string; description: string }> = ({
  icon,
  title,
  description,
}) => (
  <Box sx={{ textAlign: 'center', px: 2, flex: '1 1 200px', maxWidth: 260 }}>
    <Box sx={{ color: 'primary.main', mb: 1 }}>{icon}</Box>
    <Typography variant="subtitle2" sx={{ color: '#fff', mb: 0.5 }}>
      {title}
    </Typography>
    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
      {description}
    </Typography>
  </Box>
);

const QuickJoinPage: FC = () => {
  return (
    <div>
      <RedirectToHomeIfSignedIn />

      {/* Hero section */}
      <Box sx={{ mt: { xs: 2, sm: 4 }, mb: 4 }}>
        <Box
          component="img"
          src="/logo192.png"
          alt="Djambi"
          sx={{ width: 80, height: 80, mb: 2, opacity: 0.9 }}
        />
        <Typography variant="h3" sx={{ fontWeight: 700, color: '#fff', letterSpacing: '-0.02em' }}>
          Djambi
        </Typography>
        <Typography
          variant="subtitle1"
          sx={{ color: 'text.secondary', mt: 0.5, fontStyle: 'italic' }}
        >
          Machiavelli's Chessboard
        </Typography>
        <Typography variant="body2" sx={{ color: 'text.secondary', mt: 1.5, maxWidth: 420, mx: 'auto' }}>
          A multiplayer strategy game of politics, betrayal, and tactical cunning for 3-8 players on a hexagonal board.
        </Typography>
      </Box>

      {/* Join form */}
      <Container maxWidth="xs">
        <Paper
          elevation={2}
          sx={{
            p: 3,
            backgroundColor: 'rgba(255,255,255,0.04)',
            borderRadius: 2,
            border: '1px solid rgba(255,255,255,0.08)',
          }}
        >
          <Typography variant="h6" sx={{ color: '#fff', mb: 0.5 }}>
            Jump in
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
            Pick a username to start playing. No password needed.
          </Typography>
          <QuickJoinForm />
        </Paper>
        <Typography variant="body2" sx={{ mt: 2, color: 'text.secondary' }}>
          Already have an account?{' '}
          <Link component="button" variant="body2" onClick={() => navigateTo(Routes.magicLink)}>
            Sign in on this device
          </Link>
        </Typography>
      </Container>

      {/* Feature highlights */}
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          justifyContent: 'center',
          gap: 3,
          mt: 6,
          mb: 2,
          maxWidth: 640,
          mx: 'auto',
        }}
      >
        <FeatureItem
          icon={<GroupsIcon fontSize="large" />}
          title="3-8 Players"
          description="Dynamic alliances on a board that scales with the player count"
        />
        <FeatureItem
          icon={<ExtensionIcon fontSize="large" />}
          title="7 Unique Pieces"
          description="Each piece type has its own movement and capture rules"
        />
        <FeatureItem
          icon={<LinkIcon fontSize="large" />}
          title="Invite Friends"
          description="Create a private game and share the link"
        />
        <FeatureItem
          icon={<VisibilityIcon fontSize="large" />}
          title="Watch Games"
          description="Spectate any public game in progress"
        />
      </Box>
    </div>
  );
};

export default QuickJoinPage;
