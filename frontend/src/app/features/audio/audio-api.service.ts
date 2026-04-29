import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface ReciterInfo {
  code: string;
  name: string;
  arabicName: string;
  attribution: string;
}

export interface AyahTiming {
  numberInSurah: number;
  fromMs: number;
  toMs: number;
}

export interface AudioRecitation {
  surahId: number;
  reciter: ReciterInfo;
  audioUrl: string;
  hasTimings: boolean;
  ayahTimings: AyahTiming[];
}

@Injectable({ providedIn: 'root' })
export class AudioApiService {
  private readonly http = inject(HttpClient);

  getRecitation(surahId: number, reciter = 'ar.alafasy'): Observable<AudioRecitation> {
    return this.http.get<AudioRecitation>(`/api/v1/audio/${surahId}`, { params: { reciter } });
  }
}
