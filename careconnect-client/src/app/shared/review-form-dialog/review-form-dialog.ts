import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { SaveReviewRequest } from '../../core/models/review.model';

export interface ReviewFormDialogData {
  title: string;
  targetName: string;
  rating?: number;
  comment?: string | null;
  hidden?: boolean;
}

@Component({
  selector: 'app-review-form-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>Verified completed interaction with <strong>{{ data.targetName }}</strong>.</p>
      @if (data.hidden) { <p class="cc-notice">Hidden by moderation. Editing will not restore public visibility.</p> }
      <fieldset>
        <legend>Rating (required)</legend>
        <div class="stars" role="radiogroup" aria-label="Rating from 1 to 5">
          @for (star of stars; track star) {
            <button type="button" mat-icon-button role="radio"
              [attr.aria-checked]="form.controls.rating.value === star"
              [attr.aria-label]="star + ' stars'" (click)="form.controls.rating.setValue(star)">
              <mat-icon>{{ star <= form.controls.rating.value ? 'star' : 'star_border' }}</mat-icon>
            </button>
          }
        </div>
        <span>{{ form.controls.rating.value }} of 5 selected</span>
      </fieldset>
      <mat-form-field>
        <mat-label>Comment (optional)</mat-label>
        <textarea matInput rows="6" maxlength="2000" [formControl]="form.controls.comment"></textarea>
        <mat-hint align="end">{{ form.controls.comment.value.length }}/2000</mat-hint>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="submit()">
        {{ data.rating ? 'Update review' : 'Submit review' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content { min-width: min(520px, 75vw); display: grid; gap: 16px; }
    fieldset { border: 1px solid var(--cc-border, #d7e2e0); border-radius: 12px; padding: 12px; }
    .stars { display: flex; }
    .stars mat-icon { color: #b77b00; }
    mat-form-field { width: 100%; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewFormDialog {
  protected readonly data = inject<ReviewFormDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ReviewFormDialog, SaveReviewRequest>);
  protected readonly stars = [1, 2, 3, 4, 5];
  protected readonly form = new FormGroup({
    rating: new FormControl(this.data.rating ?? 0, { nonNullable: true, validators: [Validators.min(1), Validators.max(5)] }),
    comment: new FormControl(this.data.comment ?? '', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.ref.close({
      rating: this.form.controls.rating.value,
      comment: this.form.controls.comment.value.trim() || null,
    });
  }
}
