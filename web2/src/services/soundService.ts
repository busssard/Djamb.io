let gongAudio: HTMLAudioElement | null = null;

export function preloadGong(): void {
  gongAudio = new Audio('/sounds/gong.wav');
  gongAudio.preload = 'auto';
}

export function playGong(): void {
  if (gongAudio) {
    gongAudio.currentTime = 0;
    gongAudio.play().catch(() => {}); // Browser may block before user interaction
  }
}
