import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./search.page').then((m) => m.SearchPage),
  },
];
