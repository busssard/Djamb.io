import { PieceImageInfo } from '../../model/images';
import { PieceImageLoadedAction, PieceImagesClearedAction } from './actions';
import { ImagesActionTypes } from './actionTypes';

export function pieceImageLoadedAction(info: PieceImageInfo): PieceImageLoadedAction {
  return {
    type: ImagesActionTypes.PieceImageLoaded,
    info,
  };
}

export function pieceImagesClearedAction(): PieceImagesClearedAction {
  return {
    type: ImagesActionTypes.PieceImagesCleared,
  };
}
