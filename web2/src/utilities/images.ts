import { PieceKind } from '../api-client';

export function getPieceImageKey(kind: PieceKind, colorId: number | null): string {
  return colorId !== null ? `${kind}${colorId}` : `${kind}Neutral`;
}

export function getLiegelordImageKey(colorId: number | null): string {
  return colorId !== null ? `Liegelord${colorId}` : `LiegelordNeutral`;
}

export function getPoweredConduitImageKey(colorId: number | null): string {
  return colorId !== null ? `ConduitPowered${colorId}` : `ConduitPoweredNeutral`;
}

function imageToCanvas(image: HTMLImageElement | HTMLCanvasElement): HTMLCanvasElement {
  const c = document.createElement('canvas');
  c.width = image.width;
  c.height = image.height;
  const ctx = c.getContext('2d');
  if (!ctx) {
    throw Error('Could not get canvas 2D context.');
  }
  ctx.drawImage(image, 0, 0);
  return c;
}

export function canvasToImage(canvas: HTMLCanvasElement): HTMLImageElement {
  const i = new Image();
  i.src = canvas.toDataURL('image/png');
  return i;
}

interface RgbColor {
  r: number;
  g: number;
  b: number;
}

// See https://stackoverflow.com/questions/1573053/javascript-function-to-convert-color-names-to-hex-codes
// regarding colorname conversions

function colorStringToRgb(color: string): RgbColor {
  const cvs = document.createElement('canvas');
  cvs.height = 1;
  cvs.width = 1;

  const ctx = cvs.getContext('2d');
  if (!ctx) {
    throw Error('Could not get canvas 2D context.');
  }
  ctx.fillStyle = color;
  ctx.fillRect(0, 0, 1, 1);

  const { data } = ctx.getImageData(0, 0, 1, 1);
  return {
    r: data[0],
    g: data[1],
    b: data[2],
  };
}

// Convert RGB to HSL. Returns h in [0,360), s and l in [0,1].
function rgbToHsl(r: number, g: number, b: number): { h: number; s: number; l: number } {
  const rn = r / 255;
  const gn = g / 255;
  const bn = b / 255;
  const max = Math.max(rn, gn, bn);
  const min = Math.min(rn, gn, bn);
  const l = (max + min) / 2;

  if (max === min) return { h: 0, s: 0, l };

  const d = max - min;
  const s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
  let h = 0;
  if (max === rn) h = ((gn - bn) / d + (gn < bn ? 6 : 0)) * 60;
  else if (max === gn) h = ((bn - rn) / d + 2) * 60;
  else h = ((rn - gn) / d + 4) * 60;

  return { h, s, l };
}

// Convert HSL back to RGB. h in [0,360), s and l in [0,1].
function hslToRgb(h: number, s: number, l: number): RgbColor {
  if (s === 0) {
    const v = Math.round(l * 255);
    return { r: v, g: v, b: v };
  }
  const hue2rgb = (p: number, q: number, t: number) => {
    let tn = t;
    if (tn < 0) tn += 1;
    if (tn > 1) tn -= 1;
    if (tn < 1 / 6) return p + (q - p) * 6 * tn;
    if (tn < 1 / 2) return q;
    if (tn < 2 / 3) return p + (q - p) * (2 / 3 - tn) * 6;
    return p;
  };
  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  return {
    r: Math.round(hue2rgb(p, q, h / 360 + 1 / 3) * 255),
    g: Math.round(hue2rgb(p, q, h / 360) * 255),
    b: Math.round(hue2rgb(p, q, h / 360 - 1 / 3) * 255),
  };
}

/**
 * Replace all pixels matching oldColor's hue with newColor,
 * preserving the original pixel's lightness and saturation ratio.
 * Works on canvas to avoid intermediate Image load issues.
 */
export function replaceColor(
  source: HTMLImageElement | HTMLCanvasElement,
  oldColor: string,
  newColor: string,
): HTMLCanvasElement {
  const c = imageToCanvas(source);
  const ctx = c.getContext('2d');
  if (!ctx) {
    throw Error('Could not get canvas 2D context.');
  }
  const imageData = ctx.getImageData(0, 0, c.width, c.height);
  const d = imageData.data;

  const oldRgb = colorStringToRgb(oldColor);
  const newRgb = colorStringToRgb(newColor);
  const oldHsl = rgbToHsl(oldRgb.r, oldRgb.g, oldRgb.b);
  const newHsl = rgbToHsl(newRgb.r, newRgb.g, newRgb.b);

  for (let i = 0; i < d.length; i += 4) {
    const r = d[i];
    const g = d[i + 1];
    const b = d[i + 2];
    const a = d[i + 3];

    // Skip fully transparent pixels
    if (a === 0) continue;

    const pxHsl = rgbToHsl(r, g, b);

    // Check if this pixel's hue is close to the old color's hue,
    // and has meaningful saturation (not grey/black/white)
    const hueDiff = Math.abs(pxHsl.h - oldHsl.h);
    const hueClose = hueDiff < 30 || hueDiff > 330; // wraps around 360

    if (hueClose && pxHsl.s > 0.2) {
      // Replace hue with new color's hue, scale saturation, preserve lightness
      const result = hslToRgb(newHsl.h, newHsl.s * (pxHsl.s / oldHsl.s), pxHsl.l);
      d[i] = Math.min(255, Math.max(0, result.r));
      d[i + 1] = Math.min(255, Math.max(0, result.g));
      d[i + 2] = Math.min(255, Math.max(0, result.b));
    }
  }

  ctx.putImageData(imageData, 0, 0);
  return c;
}

/**
 * Add an outline around non-transparent pixels by drawing the image
 * at offsets in a circle, filling with the outline color, then drawing
 * the original on top. Works on canvas directly.
 */
export function addOutline(
  source: HTMLImageElement | HTMLCanvasElement,
  thickness: number = 2,
  color: string = 'white',
): HTMLCanvasElement {
  const pad = thickness;
  const w = source.width + pad * 2;
  const h = source.height + pad * 2;

  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');
  if (!ctx) throw Error('Could not get canvas 2D context.');

  // Collect offsets within a circle of the given thickness
  const offsets: [number, number][] = [];
  for (let dx = -thickness; dx <= thickness; dx++) {
    for (let dy = -thickness; dy <= thickness; dy++) {
      if (dx === 0 && dy === 0) continue;
      if (dx * dx + dy * dy <= thickness * thickness) {
        offsets.push([dx, dy]);
      }
    }
  }

  // Draw the image at each offset to build the outline silhouette
  ctx.globalCompositeOperation = 'source-over';
  for (const [dx, dy] of offsets) {
    ctx.drawImage(source, pad + dx, pad + dy);
  }

  // Fill all drawn pixels with the outline color
  ctx.globalCompositeOperation = 'source-in';
  ctx.fillStyle = color;
  ctx.fillRect(0, 0, w, h);

  // Draw original image on top
  ctx.globalCompositeOperation = 'source-over';
  ctx.drawImage(source, pad, pad);

  return c;
}
