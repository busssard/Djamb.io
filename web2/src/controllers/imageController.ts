import { PieceKind } from '../api-client';
import { PieceImageInfo } from '../model/images';
import { getSkin } from '../model/pieceSkins';
import { store } from '../redux';
import { pieceImageLoadedAction, pieceImagesClearedAction } from '../redux/images/actionFactory';
import { pieceColors } from '../styles/styles';
import { replaceColor, addOutline, canvasToImage } from '../utilities/images';

const minPlayerColorId = 0;
const maxPlayerColorId = 7;

function getPieceImagePath(kind: PieceKind, skinPath: string): string {
  switch (kind) {
    case PieceKind.Conduit:
      return `${skinPath}/conduit.png`;
    case PieceKind.Corpse:
      return `${skinPath}/corpse.png`;
    case PieceKind.Diplomat:
      return `${skinPath}/diplomat.png`;
    case PieceKind.Hunter:
      return `${skinPath}/hunter.png`;
    case PieceKind.Reaper:
      return `${skinPath}/reaper.png`;
    case PieceKind.Scientist:
      return `${skinPath}/scientist.png`;
    case PieceKind.Thug:
      return `${skinPath}/thug.png`;
    default:
      throw Error(`Invalid piece kind: ${kind}`);
  }
}

function createPieceImage(
  kind: PieceKind,
  colorId: number | null,
  skinPath: string,
  placeholderColor: string,
): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const image = new (window as any).Image() as HTMLImageElement;
  image.src = getPieceImagePath(kind, skinPath);
  image.onload = () => {
    // Pipeline: color replace → outline → convert to Image
    // All intermediate steps use HTMLCanvasElement to avoid async Image loads.
    let canvas: HTMLCanvasElement;

    if (kind !== PieceKind.Corpse) {
      canvas = replaceColor(image, placeholderColor, pieceColors.getPlayer(colorId));
    } else {
      // Corpse has no team color — start from the raw image
      const c = document.createElement('canvas');
      c.width = image.width;
      c.height = image.height;
      c.getContext('2d')!.drawImage(image, 0, 0);
      canvas = c;
    }

    // Add white outline for visibility on the dark board
    canvas = addOutline(canvas, 2, 'white');

    // Final conversion to HTMLImageElement for Konva
    const finalImage = canvasToImage(canvas);
    const dispatch = () => {
      const info: PieceImageInfo = {
        kind,
        playerColorId: colorId,
        image: finalImage,
      };
      store.dispatch(pieceImageLoadedAction(info));
    };

    if (finalImage.complete) {
      dispatch();
    } else {
      finalImage.onload = dispatch;
    }
  };
}

function createPieceImageForEachPlayerColor(
  kind: PieceKind,
  skinPath: string,
  placeholderColor: string,
): void {
  for (let colorId = minPlayerColorId; colorId <= maxPlayerColorId; colorId += 1) {
    createPieceImage(kind, colorId, skinPath, placeholderColor);
  }
  createPieceImage(kind, null, skinPath, placeholderColor); // Neutral sprite for abandoned pieces
}

export async function preloadAllPieceImages(): Promise<void> {
  const skinId = store.getState().config.user.pieceSkin;
  const skin = getSkin(skinId);

  const kinds = [
    PieceKind.Hunter,
    PieceKind.Conduit,
    PieceKind.Diplomat,
    PieceKind.Reaper,
    PieceKind.Scientist,
    PieceKind.Thug,
  ];

  kinds.forEach((k) => createPieceImageForEachPlayerColor(k, skin.path, skin.placeholderColor));

  createPieceImage(PieceKind.Corpse, null, skin.path, skin.placeholderColor); // Corpses are only ever neutral
}

export function reloadPieceImages(): void {
  store.dispatch(pieceImagesClearedAction());
  preloadAllPieceImages();
}
