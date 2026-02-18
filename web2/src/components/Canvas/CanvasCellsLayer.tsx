import React, { FC, useEffect, useState } from 'react';
import { Layer } from 'react-konva';
import { Animation } from 'konva/lib/Animation';
import { IFrame } from 'konva/lib/types';
import { BoardView, CellType, CellView, PieceView } from '../../board/model';
import { BoardTooltipState } from './model';
import CanvasCell from './CanvasCell/CanvasCell';
import { getBoardPieceScale } from '../../board/canvasTransformService';
import { getPieceImageKey, getLiegelordImageKey, getPoweredConduitImageKey } from '../../utilities/images';

interface Props {
  board: BoardView;
  selectCell: (cell: CellView) => void;
  pieceImages: Map<string, HTMLImageElement>;
  fieldOfPowerImage: HTMLImageElement | null;
  scale: number;
  setTooltip: (state: BoardTooltipState) => void;
  showBoardTooltip: boolean;
}

const CanvasCellsLayer: FC<Props> = ({
  board,
  selectCell,
  pieceImages,
  fieldOfPowerImage,
  scale,
  setTooltip,
  showBoardTooltip,
}) => {
  function getPieceImage(piece: PieceView | null): HTMLImageElement | null {
    if (!piece) {
      return null;
    }
    const key = piece.isLiegelord
      ? getLiegelordImageKey(piece.colorId)
      : getPieceImageKey(piece.kind, piece.colorId);
    return pieceImages.get(key) || null;
  }

  function getPoweredPieceImage(piece: PieceView | null): HTMLImageElement | null {
    if (!piece || !piece.isPoweredThisTurn) {
      return null;
    }
    return pieceImages.get(getPoweredConduitImageKey(piece.colorId)) || null;
  }

  const [highlightOpacity, setHighlightOpacity] = useState(0);
  const [powerPulsePhase, setPowerPulsePhase] = useState(0);

  useEffect(() => {
    const period = 0.5; // sec
    const maxOpactiy = 0.5;
    const pulsePeriod = 1.2; // sec — full expand+contract cycle

    const a = new Animation((frame?: IFrame) => {
      if (!frame) {
        return;
      }
      const timeSec = frame.time / 1000;
      const opacity = Math.abs(Math.sin(timeSec / period)) * maxOpactiy;
      setHighlightOpacity(opacity);
      // Power pulse: 0 → 1 → 0 smooth cycle
      const phase = Math.abs(Math.sin((timeSec / pulsePeriod) * Math.PI));
      setPowerPulsePhase(phase);
    });

    a.start();

    return () => {
      a.stop();
    };
  }, []);

  const pieceSize = scale * getBoardPieceScale(board);
  return (
    <Layer>
      {board.cells.map((c, i) => (
        <CanvasCell
          key={i.toString()}
          cell={c}
          highlightOpacity={c.isSelectable ? highlightOpacity : 0}
          selectCell={selectCell}
          pieceSize={pieceSize}
          pieceImage={getPieceImage(c.piece)}
          poweredPieceImage={getPoweredPieceImage(c.piece)}
          powerPulsePhase={powerPulsePhase}
          fieldOfPowerImage={c.type === CellType.Center ? fieldOfPowerImage : null}
          setTooltip={setTooltip}
          showBoardTooltip={showBoardTooltip}
        />
      ))}
    </Layer>
  );
};
export default CanvasCellsLayer;
