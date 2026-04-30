import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'sign-in',
    loadComponent: () => import('./sign-in.page').then((m) => m.SignInPage),
  },
  {
    path: 'register',
    loadComponent: () => import('./register.page').then((m) => m.RegisterPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'sign-in' },
];
