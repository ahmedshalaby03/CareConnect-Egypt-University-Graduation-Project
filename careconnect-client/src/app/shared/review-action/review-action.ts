import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Review, ReviewEligibility, ReviewType } from '../../core/models/review.model';
import { ReviewService } from '../../core/services/review.service';
import { NotificationService } from '../../core/services/notification.service';
import { friendlyMessageOf } from '../../core/interceptors/error.interceptor';
import { ReviewFormDialog, ReviewFormDialogData } from '../review-form-dialog/review-form-dialog';

@Component({
  selector: 'app-review-action',
  imports: [MatButtonModule, MatIconModule],
  template: `
    @if (eligibility()?.isEligible) {
      <button mat-stroked-button color="primary" [disabled]="busy()" (click)="open()">
        <mat-icon>rate_review</mat-icon>
        {{ eligibility()!.hasReview ? 'Edit ' : 'Review ' }}{{ label() }}
      </button>
      @if (review()?.moderationStatus === 2) { <span class="cc-status-chip cc-status-chip--inactive">Hidden by moderation</span> }
    }
  `,
  styles: [`:host{display:flex;align-items:center;gap:10px;flex-wrap:wrap}`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewAction implements OnInit {
  private readonly api=inject(ReviewService);private readonly dialog=inject(MatDialog);private readonly notify=inject(NotificationService);
  readonly type=input.required<ReviewType>();readonly sourceId=input.required<string>();readonly targetName=input.required<string>();readonly label=input.required<string>();
  protected readonly eligibility=signal<ReviewEligibility|null>(null);protected readonly review=signal<Review|null>(null);protected readonly busy=signal(false);
  ngOnInit(){this.load()}
  protected open(){const existing=this.review();const data:ReviewFormDialogData={title:existing?'Edit verified review':'Submit verified review',targetName:this.targetName(),rating:existing?.rating,comment:existing?.comment,hidden:existing?.moderationStatus===2};
    this.dialog.open<ReviewFormDialog,ReviewFormDialogData,any>(ReviewFormDialog,{data}).afterClosed().subscribe(value=>{if(!value)return;this.busy.set(true);this.api.save(this.type(),this.sourceId(),value,!!existing).subscribe({next:r=>{this.busy.set(false);this.review.set(r.data!);this.eligibility.update(e=>e?{...e,hasReview:true,reviewId:r.data!.id}:e);this.notify.success(r.message)},error:e=>{this.busy.set(false);this.notify.error(friendlyMessageOf(e,'Could not save review.'))}})})}
  private load(){this.api.eligibility(this.type(),this.sourceId()).subscribe({next:e=>{this.eligibility.set(e);if(e.hasReview)this.api.getPatientReview(this.type(),this.sourceId()).subscribe({next:r=>this.review.set(r)})},error:()=>undefined})}
}
