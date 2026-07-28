import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RatingSummary, Review, ReviewType } from '../../core/models/review.model';
import { ReviewService } from '../../core/services/review.service';

@Component({
  selector: 'app-rating-panel',
  imports: [DatePipe, DecimalPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="cc-card reviews-panel">
      <h2>Verified patient reviews</h2>
      @if (loading()) { <mat-spinner diameter="32"/> }
      @else if (summary(); as score) {
        @if (score.reviewCount === 0) { <div class="cc-empty-state"><mat-icon>star_outline</mat-icon><p>No reviews yet</p></div> }
        @else {
          <div class="score"><strong>{{ score.averageRating | number:'1.1-1' }} ★</strong><span>{{ score.reviewCount }} reviews</span></div>
          @for (row of distribution(score); track row.star) {
            <div class="distribution"><span>{{ row.star }} stars</span><progress [value]="row.count" [max]="score.reviewCount"></progress><span>{{ row.count }}</span></div>
          }
          <div class="review-list">
            @for (review of reviews(); track review.id) {
              <article><header><strong>{{ review.patientDisplayName }}</strong><span>{{ review.rating }} ★</span></header>
                <small><mat-icon>verified</mat-icon> Verified interaction · {{ review.createdAt | date:'mediumDate' }}</small>
                @if (review.comment) { <p>{{ review.comment }}</p> }
              </article>
            }
          </div>
          @if (reviews().length < totalReviews()) {
            <button mat-stroked-button type="button" (click)="loadMore()">Load more reviews</button>
          }
        }
      }
    </section>
  `,
  styles: [`
    .reviews-panel { display:grid; gap:14px; margin-top:20px; }
    .score { display:flex; align-items:baseline; gap:12px; } .score strong{font-size:2rem;color:#9b6800}
    .distribution{display:grid;grid-template-columns:60px 1fr 30px;gap:10px;align-items:center}
    progress{width:100%;accent-color:#b77b00}.review-list{display:grid;gap:12px}
    article{border-top:1px solid #dce7e5;padding-top:12px} header{display:flex;justify-content:space-between}
    small{display:flex;align-items:center;gap:4px;color:#526461} small mat-icon{font-size:17px;width:17px;height:17px}
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatingPanel implements OnInit {
  private readonly api = inject(ReviewService);
  readonly type = input.required<ReviewType>();
  readonly targetId = input.required<string>();
  protected readonly summary = signal<RatingSummary | null>(null);
  protected readonly reviews = signal<Review[]>([]);
  protected readonly totalReviews = signal(0);
  protected readonly loading = signal(true);
  private pageNumber = 1;

  ngOnInit(): void {
    this.api.getPublicSummary(this.type(), this.targetId()).subscribe({
      next: summary => {
        this.summary.set(summary);
        if (!summary.reviewCount) { this.loading.set(false); return; }
        this.api.getPublicReviews(this.type(), this.targetId(), { page: 1, pageSize: 5, sortBy: 'newest' }).subscribe({
          next: page => {
            this.reviews.set(page.items);
            this.totalReviews.set(page.totalCount);
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadMore(): void {
    this.pageNumber++;
    this.api.getPublicReviews(this.type(), this.targetId(), {
      page: this.pageNumber,
      pageSize: 5,
      sortBy: 'newest',
    }).subscribe(page => {
      this.reviews.update(current => [...current, ...page.items]);
      this.totalReviews.set(page.totalCount);
    });
  }

  protected distribution(summary: RatingSummary) {
    return [
      { star: 5, count: summary.distribution.fiveStars },
      { star: 4, count: summary.distribution.fourStars },
      { star: 3, count: summary.distribution.threeStars },
      { star: 2, count: summary.distribution.twoStars },
      { star: 1, count: summary.distribution.oneStar },
    ];
  }
}
