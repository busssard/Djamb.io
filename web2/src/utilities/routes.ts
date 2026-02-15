export const join = '/join';

export const home = '/home';

export const invite = (code: string): string => `/invite/${code}`;
export const inviteTemplate = '/invite/:code';

export const magicLink = '/magic-link';

export const magicLinkVerifyTemplate = '/auth/verify/:token';

export const newGame = '/new-game';

export const notifications = '/notifications';

export const rules = '/rules';

export const searchGames = '/search-games';

export const settings = '/settings';

export const signOut = '/sign-out';

export const game = (gameId: number): string => `/games/${gameId}`;
export const gameTemplate = '/games/:gameId';

export const gameDiplomacy = (gameId: number): string => `/games/${gameId}/diplomacy`;
export const gameDiplomacyTemplate = '/games/:gameId/diplomacy';

export const gameInfo = (gameId: number): string => `/games/${gameId}/info`;
export const gameInfoTemplate = '/games/:gameId/info';

export const gameLobby = (gameId: number): string => `/games/${gameId}/lobby`;
export const gameLobbyTemplate = '/games/:gameId/lobby';

export const gameOutcome = (gameId: number): string => `/games/${gameId}/outcome`;
export const gameOutcomeTemplate = '/games/:gameId/outcome';

export const gamePlay = (gameId: number): string => `/games/${gameId}/play`;
export const gamePlayTemplate = '/games/:gameId/play';

export const gameSnapshots = (gameId: number): string => `/games/${gameId}/snapshots`;
export const gameSnapshotsTemplate = '/games/:gameId/snapshots';
