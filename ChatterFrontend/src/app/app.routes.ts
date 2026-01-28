import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./test-pages/test-general/test-general').then(x => x.TestGeneral) },
];
