import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { catchError, of } from 'rxjs';

import { TafsirApiService, TafsirEntry } from './tafsir-api.service';
import { TafsirPanelService } from './tafsir-panel.service';

type Status = 'idle' | 'loading' | 'loaded' | 'not-available' | 'error';

@Component({
  selector: 'app-tafsir-panel',
  standalone: true,
  imports: [TranslateModule],
  templateUrl: './tafsir-panel.component.html',
  styleUrls: ['./tafsir-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TafsirPanelComponent {
  private readonly api = inject(TafsirApiService);
  readonly panel = inject(TafsirPanelService);

  readonly entry = signal<TafsirEntry | null>(null);
  readonly status = signal<Status>('idle');

  readonly isOpen = computed(() => this.panel.selectedAyah() !== null);

  constructor() {
    effect(() => {
      const sel = this.panel.selectedAyah();
      if (!sel) {
        this.status.set('idle');
        this.entry.set(null);
        return;
      }
      this.status.set('loading');
      this.entry.set(null);
      this.api.getEntry(sel.surahId, sel.numberInSurah).pipe(
        catchError((err: unknown) => {
          const httpErr = err as { status?: number };
          this.status.set(httpErr.status === 404 ? 'not-available' : 'error');
          return of(null);
        }),
      ).subscribe((res) => {
        if (res) {
          this.entry.set(res);
          this.status.set('loaded');
        }
      });
    });
  }

  close(): void {
    this.panel.close();
  }
}
