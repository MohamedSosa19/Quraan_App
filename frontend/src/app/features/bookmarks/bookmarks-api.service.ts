import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Bookmark {
  id: string;
  ayahId: number;
  surahId: number;
  numberInSurah: number;
  createdAt: string;
}

export interface BookmarkPage {
  items: Bookmark[];
  page: number;
  pageSize: number;
  total: number;
}

export interface CreateBookmarkRequest {
  surahId: number;
  numberInSurah: number;
}

@Injectable({ providedIn: 'root' })
export class BookmarksApiService {
  private readonly http = inject(HttpClient);

  list(page = 1, pageSize = 50): Observable<BookmarkPage> {
    return this.http.get<BookmarkPage>('/api/v1/bookmarks', { params: { page, pageSize } });
  }

  create(req: CreateBookmarkRequest): Observable<Bookmark> {
    return this.http.post<Bookmark>('/api/v1/bookmarks', req);
  }

  remove(bookmarkId: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/bookmarks/${bookmarkId}`);
  }
}
