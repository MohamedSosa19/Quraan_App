import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./surah-list.page').then((m) => m.SurahListPage),
  },
  {
    path: ':surahId',
    loadComponent: () => import('./surah-reader.page').then((m) => m.SurahReaderPage),
  },
];
