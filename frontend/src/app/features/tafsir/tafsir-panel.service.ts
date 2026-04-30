import { Injectable, signal } from '@angular/core';

export interface SelectedAyah {
  surahId: number;
  numberInSurah: number;
}

@Injectable({ providedIn: 'root' })
export class TafsirPanelService {
  readonly selectedAyah = signal<SelectedAyah | null>(null);

  open(surahId: number, numberInSurah: number): void {
    this.selectedAyah.set({ surahId, numberInSurah });
  }

  close(): void {
    this.selectedAyah.set(null);
  }
}
