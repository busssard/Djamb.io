import React, { FC } from 'react';
import { Group, Image } from 'react-konva';
import { CellView } from '../../../board/model';
import * as Point from '../../../board/point';
import * as Cell from '../../../board/cell';

interface Props {
  size: number;
  image: HTMLImageElement | null;
  poweredImage: HTMLImageElement | null;
  powerPulsePhase: number;
  cell: CellView;
}

const CanvasCellPieceLayer: FC<Props> = ({ size, image, poweredImage, powerPulsePhase, cell }) => {
  if (!cell.piece || !image) {
    return null;
  }

  const cellCenter = Cell.centroid(cell);
  const offset = { x: -(size / 2), y: -(size / 2) };
  const pieceLocation = Point.add(cellCenter, offset);

  return (
    <>
      <Image
        image={image}
        x={pieceLocation.x}
        y={pieceLocation.y}
        height={size}
        width={size}
        shadowColor="black"
        shadowOpacity={0.5}
        opacity={1}
        shadowBlur={5}
        shadowOffsetX={5}
        shadowOffsetY={5}
        listening={false}
      />
      {poweredImage && (
        <Group
          clipFunc={(ctx) => {
            const radius = powerPulsePhase * size * 0.75;
            ctx.arc(cellCenter.x, cellCenter.y, radius, 0, Math.PI * 2, false);
          }}
          listening={false}
        >
          <Image
            image={poweredImage}
            x={pieceLocation.x}
            y={pieceLocation.y}
            height={size}
            width={size}
            listening={false}
          />
        </Group>
      )}
    </>
  );
};
export default CanvasCellPieceLayer;
