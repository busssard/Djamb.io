import React, { FC } from 'react';
import { Group } from 'react-konva';
import { KonvaEventObject } from 'konva/lib/Node';
import CanvasCellPieceLayer from './CanvasCellPieceLayer';
import CanvasCellHighlightLayer from './CanvasCellHighlightLayer';
import CanvasCellBackgroundLayer from './CanvasCellBackgroundLayer';
import CanvasCellFieldOfPowerLayer from './CanvasCellFieldOfPowerLayer';
import { BoardTooltipState } from '../model';
import { CellView } from '../../../board/model';
import { getCellViewLabel, getPieceViewLabel } from '../../../utilities/copy';

interface Props {
  cell: CellView;
  highlightOpacity: number;
  selectCell: (cell: CellView) => void;
  pieceImage: HTMLImageElement | null;
  poweredPieceImage: HTMLImageElement | null;
  powerPulsePhase: number;
  fieldOfPowerImage: HTMLImageElement | null;
  pieceSize: number;
  showBoardTooltip: boolean;
  setTooltip: (state: BoardTooltipState) => void;
}

const CanvasCell: FC<Props> = ({
  cell,
  highlightOpacity,
  selectCell,
  pieceImage,
  poweredPieceImage,
  powerPulsePhase,
  fieldOfPowerImage,
  pieceSize,
  showBoardTooltip,
  setTooltip,
}) => {
  function onClick() {
    selectCell(cell);
  }

  function updateTooltip(e: KonvaEventObject<MouseEvent>) {
    if (!showBoardTooltip) {
      return;
    }

    const offset = 5;

    const pos = {
      x: e.evt.offsetX + offset,
      y: e.evt.offsetY + offset,
    };

    let text = getCellViewLabel(cell);
    if (cell.piece) {
      text += `\n${getPieceViewLabel(cell.piece)}`;
    }

    const data = {
      visible: true,
      text,
      position: pos,
    };

    setTooltip(data);
  }

  return (
    <Group onMouseMove={updateTooltip} onClick={onClick} onTap={onClick}>
      <CanvasCellBackgroundLayer cell={cell} />
      <CanvasCellFieldOfPowerLayer cell={cell} image={fieldOfPowerImage} />
      <CanvasCellHighlightLayer cell={cell} opacity={highlightOpacity} />
      <CanvasCellPieceLayer
        cell={cell}
        size={pieceSize}
        image={pieceImage}
        poweredImage={poweredPieceImage}
        powerPulsePhase={powerPulsePhase}
      />
    </Group>
  );
};

export default CanvasCell;
