import React, { FC } from 'react';
import { Image } from 'react-konva';
import { CellView } from '../../../board/model';
import * as Cell from '../../../board/cell';
import * as Point from '../../../board/point';

interface Props {
  image: HTMLImageElement | null;
  cell: CellView;
}

const CanvasCellFieldOfPowerLayer: FC<Props> = ({ image, cell }) => {
  if (!image) {
    return null;
  }

  const bbox = Cell.boundingBox(cell);
  const cellWidth = bbox.right - bbox.left;
  const cellHeight = bbox.bottom - bbox.top;

  // Fit image within the cell, preserving aspect ratio
  const imgAspect = image.width / image.height;
  const cellAspect = cellWidth / cellHeight;

  let width: number;
  let height: number;

  if (imgAspect > cellAspect) {
    width = cellWidth * 0.85;
    height = width / imgAspect;
  } else {
    height = cellHeight * 0.85;
    width = height * imgAspect;
  }

  const cellCenter = Cell.centroid(cell);
  const offset = { x: -(width / 2), y: -(height / 2) };
  const position = Point.add(cellCenter, offset);

  return (
    <Image
      image={image}
      x={position.x}
      y={position.y}
      width={width}
      height={height}
      opacity={0.6}
      listening={false}
    />
  );
};

export default CanvasCellFieldOfPowerLayer;
