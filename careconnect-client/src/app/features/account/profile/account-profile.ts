import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { AccountProfile } from '../../../core/models/account.model';
import { ROLE_LABELS } from '../../../core/models/user.model';
import { AccountSettingsService } from '../../../core/services/account-settings.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

@Component({
  selector: 'app-account-profile',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './account-profile.html',
  styleUrl: './account-profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountProfilePage implements OnInit, OnDestroy {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly accounts = inject(AccountSettingsService);
  private readonly notify = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploading = signal(false);
  protected readonly removing = signal(false);
  protected readonly account = signal<AccountProfile | null>(null);
  protected readonly loadError = signal<string | null>(null);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrors = signal<string[]>([]);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly previewUrl = signal<string | null>(null);
  protected readonly imageFailed = signal(false);
  protected readonly roleLabel = computed(() => {
    const role = this.account()?.role;
    return role ? ROLE_LABELS[role] : '';
  });
  protected readonly displayImageUrl = computed(() =>
    this.imageFailed() ? null : (this.previewUrl() ?? this.account()?.profileImageUrl ?? null),
  );
  protected readonly initials = computed(() => {
    const name = this.account()?.fullName?.trim();
    if (!name) {
      return '?';
    }

    const parts = name.split(/\s+/).filter(Boolean);
    return (parts.length > 1
      ? `${parts[0][0]}${parts[parts.length - 1][0]}`
      : parts[0][0]
    ).toUpperCase();
  });

  protected readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    phoneNumber: [
      '',
      [Validators.maxLength(20), Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/)],
    ],
  });

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected selectImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';

    if (!file) {
      return;
    }

    if (!ALLOWED_IMAGE_TYPES.has(file.type)) {
      this.notify.error('Choose a JPEG, PNG or WebP image.');
      return;
    }

    if (file.size <= 0 || file.size > MAX_IMAGE_BYTES) {
      this.notify.error('The image must be non-empty and no larger than 5 MB.');
      return;
    }

    this.revokePreview();
    this.selectedFile.set(file);
    this.previewUrl.set(URL.createObjectURL(file));
    this.imageFailed.set(false);
  }

  protected cancelSelection(): void {
    this.selectedFile.set(null);
    this.revokePreview();
    this.imageFailed.set(false);
  }

  protected uploadImage(): void {
    const file = this.selectedFile();
    if (!file || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.clearServerErrors();
    this.accounts.uploadProfileImage(file).subscribe({
      next: (response) => {
        this.uploading.set(false);
        this.selectedFile.set(null);
        this.revokePreview();
        this.applyAccount(response.data!);
        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.uploading.set(false);
        this.setServerError(error, 'The profile image could not be uploaded.');
      },
    });
  }

  protected confirmRemoveImage(): void {
    if (!this.account()?.hasProfileImage || this.removing()) {
      return;
    }

    this.dialog
      .open(ConfirmDialog, {
        width: 'min(440px, calc(100vw - 32px))',
        data: {
          title: 'Remove profile image?',
          message: 'Your initials will be shown until you upload another profile image.',
          confirmLabel: 'Remove image',
          destructive: true,
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.removeImage();
        }
      });
  }

  protected save(): void {
    this.clearServerErrors();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notify.error('Please fix the highlighted account fields.');
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.accounts
      .updateProfile({
        fullName: value.fullName.trim(),
        phoneNumber: value.phoneNumber.trim() || null,
      })
      .subscribe({
        next: (response) => {
          this.saving.set(false);
          this.applyAccount(response.data!);
          this.notify.success(response.message);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.setServerError(error, 'Account information could not be saved.');
        },
      });
  }

  protected onImageError(): void {
    this.imageFailed.set(true);
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.accounts.getProfile().subscribe({
      next: (account) => {
        this.loading.set(false);
        this.applyAccount(account);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Account settings could not be loaded.'));
      },
    });
  }

  private removeImage(): void {
    this.removing.set(true);
    this.clearServerErrors();
    this.accounts.deleteProfileImage().subscribe({
      next: (response) => {
        this.removing.set(false);
        this.cancelSelection();
        this.applyAccount(response.data!);
        this.notify.success(response.message);
      },
      error: (error: unknown) => {
        this.removing.set(false);
        this.setServerError(error, 'The profile image could not be removed.');
      },
    });
  }

  private applyAccount(account: AccountProfile): void {
    this.account.set(account);
    this.imageFailed.set(false);
    this.form.patchValue({
      fullName: account.fullName,
      phoneNumber: account.phoneNumber ?? '',
    });
    this.form.markAsPristine();
  }

  private setServerError(error: unknown, fallback: string): void {
    this.serverError.set(friendlyMessageOf(error, fallback));
    this.serverErrors.set(validationErrorsOf(error));
  }

  private clearServerErrors(): void {
    this.serverError.set(null);
    this.serverErrors.set([]);
  }

  private revokePreview(): void {
    const value = this.previewUrl();
    if (value) {
      URL.revokeObjectURL(value);
      this.previewUrl.set(null);
    }
  }
}
