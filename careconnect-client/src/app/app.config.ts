import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { errorInterceptor } from './core/interceptors/error.interceptor';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    // withComponentInputBinding lets route `data: { role }` bind to the dashboard's input.
    provideRouter(routes, withComponentInputBinding()),
    // Order matters: jwt runs first (attaches token, handles refresh), then error normalises
    // whatever comes back so components read a single friendly message.
    provideHttpClient(withInterceptors([jwtInterceptor, errorInterceptor])),
  ],
};
