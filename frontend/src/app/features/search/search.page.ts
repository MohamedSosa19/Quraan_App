import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, catchError } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SearchApiService, SearchResponse } from './search-api.service';

const DEBOUNCE_MS = 300;

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslateModule],
  templateUrl: './search.page.html',
  styleUrls: ['./search.page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {
  private readonly api = inject(SearchApiService);
  private readonly router = inject(Router);

  readonly query = signal<string>('');
  readonly page = signal<number>(1);
  readonly response = signal<SearchResponse | null>(null);
  readonly loading = signal<boolean>(false);
  readonly error = signal<boolean>(false);

  readonly hasQuery = computed(() => this.query().trim().length > 0);
  readonly noResults = computed(() => {
    const r = this.response();
    return this.hasQuery() && !this.loading() && r != null
      && r.surahMatches.length === 0 && r.ayahMatches.length === 0;
  });
  readonly totalPages = computed(() => {
    const r = this.response();
    if (!r || r.totalAyahMatches === 0) return 1;
    return Math.max(1, Math.ceil(r.totalAyahMatches / r.pageSize));
  });

  private readonly searchTrigger$ = new Subject<{ q: string; page: number }>();

  constructor() {
    this.searchTrigger$
      .pipe(
        debounceTime(DEBOUNCE_MS),
        distinctUntilChanged((a, b) => a.q === b.q && a.page === b.page),
        switchMap(({ q, page }) => {
          if (!q.trim()) {
            this.response.set(null);
            this.loading.set(false);
            return of(null);
          }
          this.loading.set(true);
          this.error.set(false);
          return this.api.search(q.trim(), page).pipe(
            catchError(() => {
              this.error.set(true);
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((resp) => {
        this.loading.set(false);
        if (resp) this.response.set(resp);
      });
  }

  onQueryChange(q: string): void {
    this.query.set(q);
    this.page.set(1);
    this.searchTrigger$.next({ q, page: 1 });
  }

  goToAyah(surahId: number, numberInSurah: number): void {
    void this.router.navigate(['/surahs', surahId], { queryParams: { ayah: numberInSurah } });
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      const newPage = this.page() + 1;
      this.page.set(newPage);
      this.searchTrigger$.next({ q: this.query(), page: newPage });
    }
  }

  prevPage(): void {
    if (this.page() > 1) {
      const newPage = this.page() - 1;
      this.page.set(newPage);
      this.searchTrigger$.next({ q: this.query(), page: newPage });
    }
  }
}
