import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { QuranApiService, SurahSummary } from './quran-api.service';

@Component({
  selector: 'app-surah-list',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  templateUrl: './surah-list.page.html',
  styleUrls: ['./surah-list.page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurahListPage {
  private readonly api = inject(QuranApiService);

  readonly surahs = signal<SurahSummary[]>([]);
  readonly loading = signal<boolean>(true);
  readonly error = signal<boolean>(false);
  readonly hasSurahs = computed(() => this.surahs().length > 0);

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const list = await firstValueFrom(this.api.getSurahs());
      this.surahs.set(list);
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
