import { Routes } from '@angular/router';

// Each feature route module is created as part of its corresponding user
// story phase (US1: home + quran; US2 styling; US3 audio; US4 search; US5
// tafsir; US6 auth; US7 bookmarks). Until those are scaffolded the lazy
// imports below resolve to empty `routes` arrays so the build stays green.
export const routes: Routes = [
  {
    path: '',
    loadChildren: () =>
      import('./features/home/home.routes').then((m) => m.routes),
  },
  {
    path: 'surahs',
    loadChildren: () =>
      import('./features/quran/quran.routes').then((m) => m.routes),
  },
  {
    path: 'search',
    loadChildren: () =>
      import('./features/search/search.routes').then((m) => m.routes),
  },
  {
    path: 'tafsir',
    loadChildren: () =>
      import('./features/tafsir/tafsir.routes').then((m) => m.routes),
  },
  {
    path: 'bookmarks',
    loadChildren: () =>
      import('./features/bookmarks/bookmarks.routes').then((m) => m.routes),
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.routes),
  },
  { path: '**', redirectTo: '' },
];
