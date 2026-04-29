import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import type { SurahSummary } from '../quran/quran-api.service';

export interface AyahMatch {
  id: number;
  surahId: number;
  numberInSurah: number;
  arabicText: string;
  translationText: string;
  juzNumber: number;
  hizbQuarter: number;
  sajda: boolean;
  matchedIn: 'arabic' | 'translation';
  highlightSnippet: string;
}

export interface SearchResponse {
  query: string;
  surahMatches: SurahSummary[];
  ayahMatches: AyahMatch[];
  page: number;
  pageSize: number;
  totalAyahMatches: number;
}

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);

  search(q: string, page = 1, pageSize = 20, translation = 'en.sahih'): Observable<SearchResponse> {
    const params = new HttpParams()
      .set('q', q)
      .set('page', page)
      .set('pageSize', pageSize)
      .set('translation', translation);
    return this.http.get<SearchResponse>('/api/v1/search', { params });
  }
}
