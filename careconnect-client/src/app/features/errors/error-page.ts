import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

const COPY: Record<string, { title: string; message: string; icon: string }> = {
  '403': {
    title: 'Access denied',
    message: 'Your account does not have permission to open this page.',
    icon: 'lock',
  },
  '404': {
    title: 'Page not found',
    message: 'The page may have moved, or the address may be incorrect.',
    icon: 'search_off',
  },
  error: {
    title: 'Something went wrong',
    message: 'CareConnect could not load this page. Please try again.',
    icon: 'cloud_off',
  },
};

@Component({
  selector: 'app-error-page',
  imports: [RouterLink, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="error-page">
      <section class="cc-card" role="alert">
        <span class="error-page__icon" aria-hidden="true">
          <mat-icon>{{ copy().icon }}</mat-icon>
        </span>
        <span class="error-page__code">{{ code() }}</span>
        <h1>{{ copy().title }}</h1>
        <p>{{ copy().message }}</p>
        <div class="error-page__actions">
          <a mat-flat-button routerLink="/">Go to my dashboard</a>
          <button mat-stroked-button type="button" (click)="goBack()">Go back</button>
        </div>
      </section>
    </main>
  `,
  styles: `
    :host{display:block;min-height:calc(100dvh - 64px)}
    .error-page{display:grid;place-items:center;min-height:inherit;padding:24px}
    .error-page .cc-card{width:min(520px,100%);padding:clamp(28px,6vw,52px);text-align:center}
    .error-page__icon{display:grid;place-items:center;width:72px;height:72px;margin:0 auto 14px;border-radius:22px;background:color-mix(in srgb,var(--cc-brand) 12%,transparent);color:var(--cc-brand)}
    .error-page__icon mat-icon{width:38px;height:38px;font-size:38px}
    .error-page__code{font-weight:800;letter-spacing:.12em;color:var(--cc-brand)}
    h1{margin:8px 0;font-size:clamp(1.55rem,4vw,2.1rem)}
    p{margin:0 auto 24px;max-width:42ch;color:var(--mat-sys-on-surface-variant);line-height:1.6}
    .error-page__actions{display:flex;justify-content:center;gap:12px;flex-wrap:wrap}
  `,
})
export class ErrorPage {
  readonly code = input('error');
  protected readonly copy = computed(() => COPY[this.code()] ?? COPY['error']);

  protected goBack(): void {
    history.length > 1 ? history.back() : location.assign('/');
  }
}
