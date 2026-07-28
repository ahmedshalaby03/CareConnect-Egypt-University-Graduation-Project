import { PagedResult } from './api-response.model';

export type ReviewType = 1 | 2 | 3;
export type ReviewModerationStatus = 1 | 2;

export const REVIEW_TYPE_LABELS: Record<ReviewType, string> = {
  1: 'Doctor',
  2: 'Hospital',
  3: 'Medical Service Provider',
};

export interface SaveReviewRequest {
  rating: number;
  comment: string | null;
}

export interface ReviewEligibility {
  isEligible: boolean;
  hasReview: boolean;
  reviewId: string | null;
  message: string;
}

export interface Review {
  id: string;
  reviewType: ReviewType;
  reviewTypeName: string;
  sourceId: string;
  sourceReference: string;
  targetId: string;
  targetName: string;
  patientDisplayName: string;
  rating: number;
  comment: string | null;
  moderationStatus: ReviewModerationStatus;
  moderationStatusName: string;
  moderationReason: string | null;
  moderatedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  isVerifiedInteraction: boolean;
}

export interface RatingDistribution {
  oneStar: number;
  twoStars: number;
  threeStars: number;
  fourStars: number;
  fiveStars: number;
}

export interface RatingSummary {
  averageRating: number | null;
  reviewCount: number;
  distribution: RatingDistribution;
}

export type ReviewPage = PagedResult<Review>;

export interface ReviewFilter {
  page: number;
  pageSize: number;
  reviewType?: ReviewType | null;
  moderationStatus?: ReviewModerationStatus | null;
  rating?: number | null;
  search?: string;
  patientName?: string;
  targetName?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
}
