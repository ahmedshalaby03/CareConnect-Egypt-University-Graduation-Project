import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
import {
  PUBLIC_ROLES,
  ROLE_DESCRIPTIONS,
  ROLE_LABELS,
  UserRole,
} from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { matchFields, strongPassword } from '../../../core/validators/match-fields.validator';

interface RoleOption {
  value: UserRole;
  label: string;
  description: string;
  icon: string;
}

const ROLE_ICONS: Record<UserRole, string> = {
  Patient: 'personal_injury',
  Doctor: 'medical_information',
  Hospital: 'local_hospital',
  MedicalServiceProvider: 'medical_services',
  SuperAdmin: 'admin_panel_settings',
};

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);
  protected readonly hidePassword = signal(true);
  protected readonly hideConfirm = signal(true);

  // Only the four public roles are ever offered; SuperAdmin is seeded and never registerable.
  protected readonly roleOptions: RoleOption[] = PUBLIC_ROLES.map((value) => ({
    value,
    label: ROLE_LABELS[value],
    description: ROLE_DESCRIPTIONS[value],
    icon: ROLE_ICONS[value],
  }));

  protected readonly form = this.fb.group(
    {
      fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/)]],
      role: ['' as UserRole | '', [Validators.required]],
      password: ['', [Validators.required, strongPassword()]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: matchFields('password', 'confirmPassword') },
  );

  protected selectRole(role: UserRole): void {
    this.form.controls.role.setValue(role);
    this.form.controls.role.markAsTouched();
  }

  protected passwordRequirements(): string[] {
    const error = this.form.controls.password.getError('weakPassword') as string[] | null;
    return error ?? [];
  }

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);

    this.auth
      .register({
        fullName: raw.fullName.trim(),
        email: raw.email.trim(),
        phoneNumber: raw.phoneNumber.trim() ? raw.phoneNumber.trim() : null,
        password: raw.password,
        confirmPassword: raw.confirmPassword,
        role: raw.role as UserRole,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.notify.success('Registration successful. You can now sign in.');
          void this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.serverError.set(friendlyMessageOf(error, 'Registration failed.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }
}
