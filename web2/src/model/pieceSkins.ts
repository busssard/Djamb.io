export type PieceSkin = {
  id: string;
  name: string;
  placeholderColor: string;
  path: string;
};

export const pieceSkins: PieceSkin[] = [
  { id: 'modern', name: 'Modern', placeholderColor: '#FF0000', path: '/pieces/modern' },
  { id: 'classic', name: 'Classic', placeholderColor: '#CCCC33', path: '/pieces/classic' },
];

export const defaultSkinId = 'modern';

export function getSkin(id: string): PieceSkin {
  return pieceSkins.find((s) => s.id === id) ?? pieceSkins[0];
}
