import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type RevelationPlace = 'Meccan' | 'Medinan';

export interface SurahSummary {
  id: number;
  arabicName: string;
  transliteratedName: string;
  englishName: string;
  revelationPlace: RevelationPlace;
  ayahCount: number;
}

export interface TranslationInfo {
  code: string;
  language: string;
  name: string;
  attribution: string;
}

export interface Ayah {
  id: number;
  surahId: number;
  numberInSurah: number;
  arabicText: string;
  translationText: string;
  juzNumber: number;
  hizbQuarter: number;
  sajda: boolean;
}

export interface SurahDetail extends SurahSummary {
  translation: TranslationInfo;
  ayahs: Ayah[];
}

const API_BASE = '/api/v1';

@Injectable({ providedIn: 'root' })
export class QuranApiService {
  private readonly http = inject(HttpClient);

  getSurahs(): Observable<SurahSummary[]> {
    return this.http.get<SurahSummary[]>(`${API_BASE}/surahs`);
  }

  getSurah(surahId: number, translation = 'en.sahih'): Observable<SurahDetail> {
    return this.http.get<SurahDetail>(`${API_BASE}/surahs/${surahId}`, {
      params: { translation },
    });
  }

  getAyah(surahId: number, numberInSurah: number, translation = 'en.sahih'): Observable<Ayah> {
    return this.http.get<Ayah>(`${API_BASE}/surahs/${surahId}/ayahs/${numberInSurah}`, {
      params: { translation },
    });
  }
}
