import { Context } from 'konva/lib/Context';
import React, { FC } from 'react';
import { Shape } from 'react-konva';
import { Polygon } from '../../board/model';

export interface CanvasPolygonStyle {
  fillColor?: string;
  strokeColor?: string;
  strokeWidth?: number;
  opacity?: number;
}

interface Props {
  polygon: Polygon;
  style: CanvasPolygonStyle;
  listening?: boolean;
}

const CanvasPolygon: FC<Props> = ({ polygon, style, listening }) => (
  <Shape
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    sceneFunc={(ctx: Context, shape: any) => {
      const vs = polygon.vertices;
      let v = vs[0];

      ctx.beginPath();
      ctx.moveTo(v.x, v.y);

      for (let i = 1; i < vs.length; i += 1) {
        v = vs[i];
        ctx.lineTo(v.x, v.y);
      }

      ctx.closePath();
      ctx.fillStrokeShape(shape);
    }}
    fill={style.fillColor}
    stroke={style.strokeColor}
    strokeWidth={style.strokeWidth}
    opacity={style.opacity !== undefined ? style.opacity : 1}
    listening={listening !== undefined ? listening : true}
  />
);
export default CanvasPolygon;
