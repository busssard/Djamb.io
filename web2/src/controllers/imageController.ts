import { PieceKind } from '../api-client';
import { PieceImageInfo } from '../model/images';
import { store } from '../redux';
import { pieceImageLoadedAction } from '../redux/images/actionFactory';
import { pieceColors } from '../styles/styles';
import { replaceColor, addOutline, canvasToImage } from '../utilities/images';

const minPlayerColorId = 0;
const maxPlayerColorId = 7;

const piecesDir = '/pieces';

function getPieceImagePath(kind: PieceKind): string {
  switch (kind) {
    case PieceKind.Conduit:
      return `${piecesDir}/conduit.png`;
    case PieceKind.Corpse:
      return `${piecesDir}/corpse.png`;
    case PieceKind.Diplomat:
      return `${piecesDir}/diplomat.png`;
    case PieceKind.Hunter:
      return `${piecesDir}/hunter.png`;
    case PieceKind.Reaper:
      return `${piecesDir}/reaper.png`;
    case PieceKind.Scientist:
      return `${piecesDir}/scientist.png`;
    case PieceKind.Thug:
      return `${piecesDir}/thug.png`;
    default:
      throw Error(`Invalid piece kind: ${kind}`);
  }
}

function createPieceImage(kind: PieceKind, colorId: number | null): void {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const image = new (window as any).Image() as HTMLImageElement;
  image.src = getPieceImagePath(kind);
  image.onload = () => {
    // Pipeline: color replace → outline → convert to Image
    // All intermediate steps use HTMLCanvasElement to avoid async Image loads.
    let canvas: HTMLCanvasElement;

    if (kind !== PieceKind.Corpse) {
      canvas = replaceColor(image, pieceColors.placeholder, pieceColors.getPlayer(colorId));
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

function createPieceImageForEachPlayerColor(kind: PieceKind): void {
  for (let colorId = minPlayerColorId; colorId <= maxPlayerColorId; colorId += 1) {
    createPieceImage(kind, colorId);
  }
  createPieceImage(kind, null); // Neutral sprite for abandoned pieces
}

export async function preloadAllPieceImages(): Promise<void> {
  const kinds = [
    PieceKind.Hunter,
    PieceKind.Conduit,
    PieceKind.Diplomat,
    PieceKind.Reaper,
    PieceKind.Scientist,
    PieceKind.Thug,
  ];

  kinds.forEach((k) => createPieceImageForEachPlayerColor(k));

  createPieceImage(PieceKind.Corpse, null); // Corpses are only ever neutral
}
