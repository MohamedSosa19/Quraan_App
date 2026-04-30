import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./bookmarks.page').then((m) => m.BookmarksPage),
  },
];
