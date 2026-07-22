import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** One place to style and configure toasts, so every screen looks the same. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 4000,
      panelClass: 'cc-snack-success',
      horizontalPosition: 'center',
      verticalPosition: 'top',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 6000,
      panelClass: 'cc-snack-error',
      horizontalPosition: 'center',
      verticalPosition: 'top',
    });
  }
}
