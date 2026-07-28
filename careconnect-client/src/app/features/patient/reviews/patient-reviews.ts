import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { Review, ReviewPage, ReviewType } from '../../../core/models/review.model';
import { ReviewService } from '../../../core/services/review.service';
import { NotificationService } from '../../../core/services/notification.service';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { ReviewFormDialog, ReviewFormDialogData } from '../../../shared/review-form-dialog/review-form-dialog';

@Component({
  selector: 'app-patient-reviews',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatSelectModule],
  template: `
    <header><div><p class="eyebrow">Verified feedback</p><h1>My Reviews</h1></div></header>
    <section class="cc-card cc-filters">
      <mat-form-field><mat-label>Review type</mat-label><mat-select [formControl]="type" (selectionChange)="reload()">
        <mat-option [value]="null">All</mat-option><mat-option [value]="1">Doctor</mat-option>
        <mat-option [value]="2">Hospital</mat-option><mat-option [value]="3">Medical service provider</mat-option>
      </mat-select></mat-form-field>
      <mat-form-field><mat-label>Rating</mat-label><mat-select [formControl]="rating" (selectionChange)="reload()">
        <mat-option [value]="null">All</mat-option>@for(star of [5,4,3,2,1];track star){<mat-option [value]="star">{{star}} stars</mat-option>}
      </mat-select></mat-form-field>
      <mat-form-field><mat-label>Visibility</mat-label><mat-select [formControl]="visibility" (selectionChange)="reload()">
        <mat-option [value]="null">All</mat-option><mat-option [value]="1">Visible</mat-option><mat-option [value]="2">Hidden</mat-option>
      </mat-select></mat-form-field>
      <mat-form-field><mat-label>Search reviews</mat-label><input matInput [formControl]="search" maxlength="150" (keyup.enter)="reload()"></mat-form-field>
      <button mat-stroked-button type="button" (click)="reload()">Search</button>
    </section>
    @if (loading()) { <div class="cc-loading">Loading reviews…</div> }
    @else if (!page()?.items?.length) { <div class="cc-empty-state"><mat-icon>rate_review</mat-icon><p>No reviews found.</p></div> }
    @else {
      <section class="cc-card-grid">@for(review of page()!.items;track review.id){
        <article class="cc-card"><header><span class="cc-role-chip">{{review.reviewTypeName}}</span><strong>{{review.rating}} ★</strong></header>
          <h2>{{review.targetName}}</h2><p>{{review.comment || 'Rating only — no written comment.'}}</p>
          <small><mat-icon>verified</mat-icon> Verified interaction · {{review.sourceReference}} · {{review.createdAt|date:'mediumDate'}}</small>
          @if(review.updatedAt){<small>Updated {{review.updatedAt|date:'mediumDate'}}</small>}
          @if(review.moderationStatus===2){<p class="cc-notice">Hidden by moderation</p>}
          <footer><a mat-stroked-button [routerLink]="sourceRoute(review)">View interaction</a>
            <button mat-flat-button color="primary" (click)="edit(review)">Edit</button></footer>
        </article>
      }</section>
      <mat-paginator [length]="page()!.totalCount" [pageSize]="10" (page)="changePage($event)"/>
    }
  `,
  styles: [`article header,article footer{display:flex;justify-content:space-between;gap:10px;align-items:center}article small{display:flex;gap:4px;align-items:center;color:#586a67}small mat-icon{font-size:17px;width:17px;height:17px}article footer{margin-top:auto}`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PatientReviews implements OnInit {
  private readonly api=inject(ReviewService);private readonly dialog=inject(MatDialog);private readonly notify=inject(NotificationService);
  protected readonly page=signal<ReviewPage|null>(null);protected readonly loading=signal(true);
  protected readonly type=new FormControl<ReviewType|null>(null);protected readonly rating=new FormControl<number|null>(null);
  protected readonly visibility=new FormControl<1|2|null>(null);protected readonly search=new FormControl('',{nonNullable:true});private pageNumber=1;
  ngOnInit(){this.load()} protected reload(){this.pageNumber=1;this.load()}
  protected changePage(e:PageEvent){this.pageNumber=e.pageIndex+1;this.load()}
  protected sourceRoute(r:Review){return r.reviewType===3?['/dashboard/patient/service-requests',r.sourceId]:['/dashboard/patient/appointments',r.sourceId]}
  protected edit(review:Review){this.dialog.open<ReviewFormDialog,ReviewFormDialogData,any>(ReviewFormDialog,{data:{title:'Edit review',targetName:review.targetName,rating:review.rating,comment:review.comment,hidden:review.moderationStatus===2}})
    .afterClosed().subscribe(value=>{if(!value)return;this.api.save(review.reviewType,review.sourceId,value,true).subscribe({next:r=>{this.notify.success(r.message);this.load()},error:e=>this.notify.error(friendlyMessageOf(e,'Could not update review.'))})})}
  private load(){this.loading.set(true);this.api.getPatientReviews({page:this.pageNumber,pageSize:10,reviewType:this.type.value,rating:this.rating.value,moderationStatus:this.visibility.value,search:this.search.value.trim(),sortBy:'newest'}).subscribe({next:p=>{this.page.set(p);this.loading.set(false)},error:()=>this.loading.set(false)})}
}
