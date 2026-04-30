import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface TafsirSource {
  code: string;
  name: string;
  attribution: string;
}

export interface TafsirEntry {
  surahId: number;
  numberInSurah: number;
  source: TafsirSource;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class TafsirApiService {
  private readonly http = inject(HttpClient);

  getEntry(surahId: number, numberInSurah: number, source = 'ibn-kathir-en'): Observable<TafsirEntry> {
    return this.http.get<TafsirEntry>(
      `/api/v1/tafsir/${surahId}/${numberInSurah}`,
      { params: { source } },
    );
  }
}
