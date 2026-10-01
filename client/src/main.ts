import { bootstrapApplication } from '@angular/platform-browser';
import localeVi from '@angular/common/locales/vi';
import { registerLocaleData } from '@angular/common';
import { appConfig } from './app/app.config';
import { App } from './app/app';

registerLocaleData(localeVi);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
