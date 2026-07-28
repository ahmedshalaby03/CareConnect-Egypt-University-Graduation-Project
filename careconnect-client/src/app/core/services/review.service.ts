import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  RatingSummary,
  Review,
  ReviewEligibility,
  ReviewFilter,
  ReviewPage,
  ReviewType,
  SaveReviewRequest,
} from '../models/review.model';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiBaseUrl;

  eligibility(type: ReviewType, sourceId: string): Observable<ReviewEligibility> {
    return this.http.get<ApiResponse<ReviewEligibility>>(`${this.patientSource(type, sourceId)}/eligibility`)
      .pipe(map(r => r.data!));
  }

  getPatientReview(type: ReviewType, sourceId: string): Observable<Review> {
    return this.http.get<ApiResponse<Review>>(this.patientSource(type, sourceId)).pipe(map(r => r.data!));
  }

  save(type: ReviewType, sourceId: string, request: SaveReviewRequest, update: boolean): Observable<ApiResponse<Review>> {
    const url = this.patientSource(type, sourceId);
    return update
      ? this.http.put<ApiResponse<Review>>(url, request)
      : this.http.post<ApiResponse<Review>>(url, request);
  }

  getPatientReviews(filter: ReviewFilter): Observable<ReviewPage> {
    return this.http.get<ApiResponse<ReviewPage>>(`${this.api}/patient/reviews`, { params: this.params(filter) })
      .pipe(map(r => r.data!));
  }

  getPublicReviews(type: ReviewType, targetId: string, filter: ReviewFilter): Observable<ReviewPage> {
    return this.http.get<ApiResponse<ReviewPage>>(`${this.api}/${this.publicResource(type)}/${targetId}/reviews`, {
      params: this.params(filter),
    }).pipe(map(r => r.data!));
  }

  getPublicSummary(type: ReviewType, targetId: string): Observable<RatingSummary> {
    return this.http.get<ApiResponse<RatingSummary>>(
      `${this.api}/${this.publicResource(type)}/${targetId}/rating-summary`,
    ).pipe(map(r => r.data!));
  }

  getOwnerReviews(ownerPath: string, filter: ReviewFilter): Observable<ReviewPage> {
    return this.http.get<ApiResponse<ReviewPage>>(`${this.api}/${ownerPath}/reviews`, {
      params: this.params(filter),
    }).pipe(map(r => r.data!));
  }

  getOwnerSummary(ownerPath: string): Observable<RatingSummary> {
    return this.http.get<ApiResponse<RatingSummary>>(`${this.api}/${ownerPath}/reviews/summary`)
      .pipe(map(r => r.data!));
  }

  getAdminReviews(filter: ReviewFilter): Observable<ReviewPage> {
    return this.http.get<ApiResponse<ReviewPage>>(`${this.api}/super-admin/reviews`, {
      params: this.params(filter),
    }).pipe(map(r => r.data!));
  }

  hide(type: ReviewType, id: string, reason: string): Observable<ApiResponse<Review>> {
    return this.http.post<ApiResponse<Review>>(
      `${this.api}/super-admin/reviews/${this.routeType(type)}/${id}/hide`, { reason },
    );
  }

  restore(type: ReviewType, id: string): Observable<ApiResponse<Review>> {
    return this.http.post<ApiResponse<Review>>(
      `${this.api}/super-admin/reviews/${this.routeType(type)}/${id}/restore`, {},
    );
  }

  private patientSource(type: ReviewType, sourceId: string): string {
    return type === 3
      ? `${this.api}/patient/medical-service-requests/${sourceId}/review`
      : `${this.api}/patient/appointments/${sourceId}/${type === 1 ? 'doctor' : 'hospital'}-review`;
  }

  private publicResource(type: ReviewType): string {
    return type === 1 ? 'doctors' : type === 2 ? 'hospitals' : 'medical-service-providers';
  }

  private routeType(type: ReviewType): string {
    return type === 1 ? 'doctor' : type === 2 ? 'hospital' : 'medical-service-provider';
  }

  private params(filter: ReviewFilter): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
    }
    return params;
  }
}
