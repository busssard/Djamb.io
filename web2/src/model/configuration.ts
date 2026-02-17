export type UserConfig = {
  logRedux: boolean;
  notificationDisplaySeconds: number;
  showCellAndPieceIds: boolean;
  showBoardTooltips: boolean;
  pieceSkin: string;
};

export type EnvironmentConfig = {
  apiUrl: string;
};

export type Config = {
  environment: EnvironmentConfig;
  user: UserConfig;
};
