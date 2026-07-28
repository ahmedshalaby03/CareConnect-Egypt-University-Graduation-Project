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
import { Review, ReviewPage, ReviewType } from '../../../core/models/review.model';
import { ReviewService } from '../../../core/services/review.service';
import { NotificationService } from '../../../core/services/notification.service';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { ReasonDialog, ReasonDialogData } from '../../../shared/reason-dialog/reason-dialog';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';

@Component({
 selector:'app-review-moderation',imports:[DatePipe,ReactiveFormsModule,MatButtonModule,MatFormFieldModule,MatIconModule,MatInputModule,MatPaginatorModule,MatSelectModule],
 template:`
  <header><p class="eyebrow">Content moderation</p><h1>Verified Reviews</h1></header>
  <section class="cc-card cc-filters"><mat-form-field><mat-label>Type</mat-label><mat-select [formControl]="type" (selectionChange)="reload()"><mat-option [value]="null">All</mat-option><mat-option [value]="1">Doctor</mat-option><mat-option [value]="2">Hospital</mat-option><mat-option [value]="3">Provider</mat-option></mat-select></mat-form-field>
  <mat-form-field><mat-label>Visibility</mat-label><mat-select [formControl]="status" (selectionChange)="reload()"><mat-option [value]="null">All</mat-option><mat-option [value]="1">Visible</mat-option><mat-option [value]="2">Hidden</mat-option></mat-select></mat-form-field>
  <mat-form-field><mat-label>Rating</mat-label><mat-select [formControl]="rating" (selectionChange)="reload()"><mat-option [value]="null">All</mat-option>@for(x of [5,4,3,2,1];track x){<mat-option [value]="x">{{x}} stars</mat-option>}</mat-select></mat-form-field>
  <mat-form-field><mat-label>Search reviews</mat-label><input matInput [formControl]="search" maxlength="150" (keyup.enter)="reload()"></mat-form-field>
  <mat-form-field><mat-label>From date</mat-label><input matInput type="date" [formControl]="dateFrom"></mat-form-field>
  <mat-form-field><mat-label>To date</mat-label><input matInput type="date" [formControl]="dateTo"></mat-form-field>
  <button mat-stroked-button type="button" (click)="reload()">Apply filters</button></section>
  @if(loading()){<div class="cc-loading">Loading reviews…</div>}@else if(!page()?.items?.length){<div class="cc-empty-state"><mat-icon>policy</mat-icon><p>No reviews found.</p></div>}
  @else{<section class="cc-card-grid">@for(r of page()!.items;track r.id){<article class="cc-card"><header><span class="cc-role-chip">{{r.reviewTypeName}}</span><strong>{{r.rating}} ★</strong></header><h2>{{r.targetName}}</h2><small>{{r.patientDisplayName}} · {{r.createdAt|date:'mediumDate'}}</small><p>{{r.comment||'Rating only'}}</p>
  <button mat-button type="button" (click)="expandedId.set(expandedId()===r.id?null:r.id)">{{expandedId()===r.id?'Hide details':'View details'}}</button>
  @if(expandedId()===r.id){<div class="cc-notice"><strong>Verified interaction:</strong> {{r.sourceReference}}<br><strong>Created:</strong> {{r.createdAt|date:'medium'}}@if(r.updatedAt){<br><strong>Updated:</strong> {{r.updatedAt|date:'medium'}}}@if(r.moderatedAt){<br><strong>Moderated:</strong> {{r.moderatedAt|date:'medium'}}}</div>}
  @if(r.moderationStatus===2){<p class="cc-notice">Hidden: {{r.moderationReason}}</p><button mat-flat-button color="primary" (click)="restore(r)">Restore</button>}@else{<button mat-stroked-button color="warn" (click)="hide(r)">Hide review</button>}</article>}</section>
  <mat-paginator [length]="page()!.totalCount" [pageSize]="10" (page)="change($event)"/>}
 `,styles:[`article header{display:flex;justify-content:space-between;align-items:center}`],changeDetection:ChangeDetectionStrategy.OnPush
})
export class ReviewModeration implements OnInit{
 private readonly api=inject(ReviewService);private readonly dialog=inject(MatDialog);private readonly notify=inject(NotificationService);
 protected readonly page=signal<ReviewPage|null>(null);protected readonly loading=signal(true);protected readonly expandedId=signal<string|null>(null);protected readonly type=new FormControl<ReviewType|null>(null);protected readonly status=new FormControl<1|2|null>(null);protected readonly rating=new FormControl<number|null>(null);
 protected readonly search=new FormControl('',{nonNullable:true});protected readonly dateFrom=new FormControl('',{nonNullable:true});protected readonly dateTo=new FormControl('',{nonNullable:true});private pageNumber=1;
 ngOnInit(){this.load()}protected reload(){this.pageNumber=1;this.load()}protected change(e:PageEvent){this.pageNumber=e.pageIndex+1;this.load()}
 protected hide(r:Review){const data:ReasonDialogData={title:'Hide review',message:'This removes the review from public lists and rating calculations.',fieldLabel:'Internal moderation reason',confirmLabel:'Hide review',minLength:1,maxLength:1000};this.dialog.open<ReasonDialog,ReasonDialogData,string>(ReasonDialog,{data}).afterClosed().subscribe(reason=>{if(!reason)return;this.api.hide(r.reviewType,r.id,reason).subscribe({next:x=>{this.notify.success(x.message);this.load()},error:e=>this.notify.error(friendlyMessageOf(e,'Could not hide review.'))})})}
 protected restore(r:Review){const data:ConfirmDialogData={title:'Restore review?',message:'The review will be public and count toward ratings again.',confirmLabel:'Restore'};this.dialog.open<ConfirmDialog,ConfirmDialogData,boolean>(ConfirmDialog,{data}).afterClosed().subscribe(ok=>{if(!ok)return;this.api.restore(r.reviewType,r.id).subscribe({next:x=>{this.notify.success(x.message);this.load()},error:e=>this.notify.error(friendlyMessageOf(e,'Could not restore review.'))})})}
 private load(){this.loading.set(true);this.api.getAdminReviews({page:this.pageNumber,pageSize:10,reviewType:this.type.value,moderationStatus:this.status.value,rating:this.rating.value,search:this.search.value.trim(),dateFrom:this.dateFrom.value,dateTo:this.dateTo.value,sortBy:'newest'}).subscribe({next:p=>{this.page.set(p);this.loading.set(false)},error:e=>{this.notify.error(friendlyMessageOf(e,'Could not load reviews.'));this.loading.set(false)}})}
}
