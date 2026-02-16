import * as MathJs from 'mathjs';
import { Point, BoardView } from './model';
import * as Transform from './transform';
import * as Pt from './point';
import * as Pl from './polygon';
import * as Rectangle from './rectangle';
import * as RegularPolygon from './regularPolygon';

export type CanvasTranformData = {
  containerSize: Point;
  canvasMargin: number;
  contentPadding: number;
  regionCount: number;
  zoomLevel: number;
};

function getBoardPolygonBaseSize(regionCount: number): Point {
  return RegularPolygon.sideToSizeRatios(regionCount);
}

export function getBoardPieceScale(board: BoardView): number {
  const size = getBoardPolygonBaseSize(board.regionCount);
  const sizeAvg = (size.x + size.y) / 2;
  const reduced = Math.sqrt(Math.max(1, sizeAvg));
  return reduced / (board.cellCountPerSide * 1.6);
}

function getTotalMargin(data: CanvasTranformData): number {
  return 2 * (data.canvasMargin + data.contentPadding);
}

// --- Scale

// These are the zoom levels available in Chrome currently
const zoomLevelScales = new Map<number, number>([
  [-7, 0.25],
  [-6, 0.33],
  [-5, 0.5],
  [-4, 0.67],
  [-3, 0.75],
  [-2, 0.8],
  [-1, 0.9],
  [0, 1.0],
  [1, 1.1],
  [2, 1.25],
  [3, 1.5],
  [4, 1.75],
  [5, 2.0],
  [6, 2.5],
  [7, 3.0],
  [8, 4.0],
  [9, 5.0],
]);

export function getZoomScaleFactor(zoomLevel: number): number {
  const result = zoomLevelScales.get(zoomLevel);
  if (result === undefined) {
    throw Error(`Invalid zoom level ${zoomLevel}`);
  }
  return result;
}

export function minZoomLevel(): number {
  return -7;
}

export function maxZoomLevel(): number {
  return 9;
}

export function getScale(data: CanvasTranformData): number {
  const contentAreaSizeWithNoZoom = Pt.subtractScalar(data.containerSize, getTotalMargin(data));
  const boardBaseSize = getBoardPolygonBaseSize(data.regionCount);
  // eslint-disable-next-line max-len
  const containerSizeScaleFactor = Rectangle.largestScaleWithinBox(
    boardBaseSize,
    contentAreaSizeWithNoZoom,
  );
  const zoomScaleFactor = getZoomScaleFactor(data.zoomLevel);
  return containerSizeScaleFactor * zoomScaleFactor;
}

//------

export function getSize(data: CanvasTranformData): Point {
  let size = getBoardPolygonBaseSize(data.regionCount);
  size = Pt.multiplyScalar(size, getScale(data));
  const totalMargin = getTotalMargin(data);
  size = Pt.add(size, { x: totalMargin, y: totalMargin });
  size = Pt.max(size, data.containerSize);
  return size;
}

// --- Transforms ---

function getTransformToScaleBoard(data: CanvasTranformData): MathJs.Matrix {
  const scale = getScale(data);
  return Transform.scale({ x: scale, y: scale });
}

function getTransformToCenterBoardInCanvas(data: CanvasTranformData): MathJs.Matrix {
  // Boardviews start with their centroid at 0,0.
  const canvasSize = getSize(data);

  let offset = Pt.multiplyScalar(canvasSize, 0.5);

  // eslint-disable-next-line max-len
  let centroidToCenterOffset = RegularPolygon.sideToCentroidOffsetFromCenterRatios(
    data.regionCount,
  );
  centroidToCenterOffset = Pt.multiplyScalar(centroidToCenterOffset, getScale(data));

  offset = Pt.add(offset, centroidToCenterOffset);

  return Transform.translate(offset);
}

export function getBoardViewTransform(data: CanvasTranformData): MathJs.Matrix {
  // Order is very important. Last transform gets applied to image first
  return Transform.compose([
    getTransformToCenterBoardInCanvas(data),
    getTransformToScaleBoard(data),
  ]);
}

export function transformBoardView(board: BoardView, data: CanvasTranformData): BoardView {
  const matrix = getBoardViewTransform(data);
  return {
    ...board,
    polygon: Pl.transform(board.polygon, matrix),
    cells: board.cells.map((c) => ({
      ...c,
      polygon: Pl.transform(c.polygon, matrix),
    })),
  };
}
