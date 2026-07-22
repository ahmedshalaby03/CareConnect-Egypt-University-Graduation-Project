import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

/**
 * Shows how close a profile is to being usable, and exactly which fields are still blank.
 * Both inputs come straight from the API, which owns the completion rule.
 */
@Component({
  selector: 'app-profile-completion',
  imports: [MatIconModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="completion" [class.completion--done]="completed()">
      <div class="completion__head">
        <mat-icon>{{ completed() ? 'verified' : 'pending_actions' }}</mat-icon>
        <div>
          <h3>{{ completed() ? 'Profile complete' : 'Profile incomplete' }}</h3>
          <p>{{ hint() }}</p>
        </div>
        <span class="completion__pct">{{ percent() }}%</span>
      </div>

      <mat-progress-bar mode="determinate" [value]="percent()" />

      @if (!completed() && missingFields().length) {
        <ul class="completion__missing">
          @for (field of missingFields(); track field) {
            <li>{{ field }}</li>
          }
        </ul>
      }
    </section>
  `,
  styles: `
    .completion {
      border: 1px solid color-mix(in srgb, var(--cc-danger) 30%, transparent);
      background: color-mix(in srgb, var(--cc-danger) 5%, transparent);
      border-radius: var(--cc-radius);
      padding: 18px 20px;
      margin-bottom: 20px;
    }

    .completion--done {
      border-color: color-mix(in srgb, var(--cc-success) 35%, transparent);
      background: color-mix(in srgb, var(--cc-success) 6%, transparent);
    }

    .completion__head {
      display: flex;
      align-items: center;
      gap: 14px;
      margin-bottom: 12px;
    }

    .completion__head mat-icon {
      color: var(--cc-danger);
      flex-shrink: 0;
    }

    .completion--done .completion__head mat-icon {
      color: var(--cc-success);
    }

    h3 {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
    }

    p {
      margin: 2px 0 0;
      font-size: 0.85rem;
      color: var(--mat-sys-on-surface-variant);
    }

    .completion__pct {
      margin-inline-start: auto;
      font-weight: 700;
      font-size: 1.1rem;
    }

    .completion__missing {
      list-style: none;
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin: 14px 0 0;
      padding: 0;
    }

    .completion__missing li {
      font-size: 0.75rem;
      font-weight: 600;
      padding: 4px 10px;
      border-radius: 999px;
      background: var(--mat-sys-surface);
      border: 1px solid var(--mat-sys-outline-variant);
    }

    @media (max-width: 520px) {
      .completion__head {
        flex-wrap: wrap;
      }
    }
  `,
})
export class ProfileCompletion {
  readonly completed = input.required<boolean>();
  readonly missingFields = input.required<string[]>();

  /** Explains why completion matters, which differs per role. */
  readonly hint = input('Complete your profile to unlock the rest of the platform.');

  /** Total number of required fields, used to turn "missing" into a percentage. */
  readonly totalRequiredFields = input(6);

  protected readonly percent = computed(() => {
    if (this.completed()) {
      return 100;
    }

    const total = this.totalRequiredFields();
    const filled = Math.max(0, total - this.missingFields().length);

    return Math.round((filled / total) * 100);
  });
}
