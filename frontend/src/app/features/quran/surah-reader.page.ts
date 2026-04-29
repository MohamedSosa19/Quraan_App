import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AyahComponent } from './ayah.component';
import { Ayah, QuranApiService, SurahDetail } from './quran-api.service';

@Component({
  selector: 'app-surah-reader',
  standalone: true,
  imports: [AyahComponent, FormsModule, ScrollingModule, TranslateModule],
  templateUrl: './surah-reader.page.html',
  styleUrls: ['./surah-reader.page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurahReaderPage {
  private readonly api = inject(QuranApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly detail = signal<SurahDetail | null>(null);
  readonly loading = signal<boolean>(true);
  readonly error = signal<boolean>(false);
  readonly surahId = signal<number>(1);
  readonly jumpInput = signal<string>('');

  readonly hasPrev = computed(() => this.surahId() > 1);
  readonly hasNext = computed(() => this.surahId() < 114);

  constructor() {
    this.route.paramMap.subscribe((p) => {
      const raw = Number.parseInt(p.get('surahId') ?? '1', 10);
      const id = Number.isFinite(raw) && raw >= 1 && raw <= 114 ? raw : 1;
      this.surahId.set(id);
    });

    this.route.queryParamMap.subscribe((q) => {
      const ayah = q.get('ayah');
      if (ayah) {
        // Defer until after detail loads.
        setTimeout(() => this.scrollToAyah(Number.parseInt(ayah, 10)), 0);
      }
    });

    effect(() => {
      const id = this.surahId();
      void this.load(id);
    });
  }

  async load(id: number): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const detail = await firstValueFrom(this.api.getSurah(id));
      this.detail.set(detail);
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  goNext(): void {
    if (this.hasNext()) {
      void this.router.navigate(['/surahs', this.surahId() + 1]);
    }
  }

  goPrev(): void {
    if (this.hasPrev()) {
      void this.router.navigate(['/surahs', this.surahId() - 1]);
    }
  }

  jump(): void {
    const raw = Number.parseInt(this.jumpInput(), 10);
    const detail = this.detail();
    if (!Number.isFinite(raw) || raw < 1 || !detail || raw > detail.ayahs.length) return;
    this.scrollToAyah(raw);
  }

  scrollToAyah(numberInSurah: number): void {
    const el = document.getElementById(`ayah-${numberInSurah}`);
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  trackById(_index: number, ayah: Ayah): number {
    return ayah.id;
  }
}
