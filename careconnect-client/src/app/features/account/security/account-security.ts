import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

function matchingPasswords(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirmation = control.get('confirmNewPassword')?.value;
  return password === confirmation ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-account-security',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './account-security.html',
  styleUrl: './account-security.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountSecurityPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly showCurrent = signal(false);
  protected readonly showNew = signal(false);
  protected readonly showConfirmation = signal(false);

  protected readonly form = this.fb.group(
    {
      currentPassword: ['', [Validators.required, Validators.maxLength(128)]],
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(128),
          Validators.pattern(/[A-Z]/),
          Validators.pattern(/[a-z]/),
          Validators.pattern(/[0-9]/),
          Validators.pattern(/[^a-zA-Z0-9]/),
        ],
      ],
      confirmNewPassword: ['', [Validators.required]],
    },
    { validators: matchingPasswords },
  );

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notify.error('Please fix the highlighted password fields.');
      return;
    }

    this.saving.set(true);
    this.auth.changePassword(this.form.getRawValue()).subscribe({
      next: (message) => {
        this.saving.set(false);
        this.form.reset();
        this.notify.success(message);

        // The API revokes every refresh token after a password change. Clear the local
        // session immediately so the next screen cannot appear authenticated with stale data.
        this.auth.forceSignOut();
        void this.router.navigate(['/login']);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.serverError.set(
          friendlyMessageOf(error, 'The password could not be changed. Check your current password.'),
        );
        this.serverErrors.set(validationErrorsOf(error));
      },
    });
  }
}
