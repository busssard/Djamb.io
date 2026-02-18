import React, { FC } from 'react';
import { Layer, Group, Rect, Text } from 'react-konva';
import { GameDto } from '../../api-client';
import { CanvasTranformData, getBoardViewTransform } from '../../board/canvasTransformService';
import * as Rpl from '../../board/regularPolygon';
import * as Pt from '../../board/point';
import { pieceColors } from '../../styles/styles';

interface Props {
  game: GameDto;
  transformData: CanvasTranformData;
}

const CanvasPlayerLabelsLayer: FC<Props> = ({ game, transformData }) => {
  const regionCount = game.parameters.regionCount;
  const boardPolygon = Rpl.create(regionCount, 1);
  const matrix = getBoardViewTransform(transformData);

  const currentPlayerId =
    game.turnCycle && game.turnCycle.length > 0 ? game.turnCycle[0] : null;

  return (
    <Layer listening={false}>
      {game.players
        ?.filter((p) => p.startingRegion != null)
        .map((player) => {
          const boardRegion = player.startingRegion!;
          if (boardRegion >= boardPolygon.vertices.length) return null;

          const outerVertex = boardPolygon.vertices[boardRegion];
          const dir = { x: outerVertex.x, y: outerVertex.y };
          const len = Math.sqrt(dir.x * dir.x + dir.y * dir.y);
          const norm = len > 0 ? { x: dir.x / len, y: dir.y / len } : { x: 0, y: -1 };
          const labelPosBoard = Pt.add(outerVertex, Pt.multiplyScalar(norm, 0.15));
          const pos = Pt.transform(labelPosBoard, matrix);

          const playerColor = pieceColors.getPlayer(player.colorId ?? null);
          const isCurrentTurn = player.id === currentPlayerId;

          const turnIndex = game.turnCycle?.indexOf(player.id) ?? -1;
          const turnOrder = turnIndex >= 0 ? turnIndex + 1 : '?';

          const nameText = player.name;
          const orderText = `#${turnOrder}`;
          const fontSize = 14;
          const smallFontSize = 11;
          const padding = 6;
          const labelWidth = Math.max(nameText.length, orderText.length) * fontSize * 0.65 + padding * 2;
          const labelHeight = fontSize + smallFontSize + 8 + padding;

          return (
            <Group
              key={player.id}
              x={pos.x}
              y={pos.y}
              offsetX={labelWidth / 2}
              offsetY={labelHeight / 2}
            >
              <Rect
                width={labelWidth}
                height={labelHeight}
                fill={isCurrentTurn ? playerColor : 'rgba(0,0,0,0.5)'}
                opacity={isCurrentTurn ? 0.3 : 0.6}
                cornerRadius={4}
                stroke="white"
                strokeWidth={1.5}
              />
              <Text
                text={nameText}
                fill={playerColor}
                fontSize={fontSize}
                fontStyle="bold"
                width={labelWidth}
                align="center"
                y={padding / 2 + 1}
              />
              <Text
                text={orderText}
                fill={playerColor}
                fontSize={smallFontSize}
                width={labelWidth}
                align="center"
                y={fontSize + padding / 2 + 3}
              />
            </Group>
          );
        })}
    </Layer>
  );
};

export default CanvasPlayerLabelsLayer;
