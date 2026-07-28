import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { RatingSummary, ReviewPage } from '../../core/models/review.model';
import { ReviewService } from '../../core/services/review.service';

@Component({
  selector:'app-owner-reviews',
  imports:[DatePipe,DecimalPipe,ReactiveFormsModule,MatButtonModule,MatFormFieldModule,MatIconModule,MatInputModule,MatPaginatorModule,MatSelectModule],
  template:`
    <header><p class="eyebrow">Verified feedback</p><h1>Patient Reviews</h1></header>
    @if(summary();as s){<section class="cc-card summary"><div><strong>{{s.averageRating===null?'—':(s.averageRating|number:'1.1-1')}}</strong><span>{{s.reviewCount?'average rating':'No reviews yet'}}</span></div>
      @for(row of rows(s);track row.star){<label>{{row.star}} stars <progress [value]="row.count" [max]="s.reviewCount||1"></progress> {{row.count}}</label>}</section>}
    <section class="cc-card cc-filters">
      <mat-form-field><mat-label>Rating</mat-label><mat-select [formControl]="rating" (selectionChange)="reload()"><mat-option [value]="null">All</mat-option>@for(x of [5,4,3,2,1];track x){<mat-option [value]="x">{{x}} stars</mat-option>}</mat-select></mat-form-field>
      <mat-form-field><mat-label>Search comments or patients</mat-label><input matInput [formControl]="search" maxlength="150" (keyup.enter)="reload()"></mat-form-field>
      <mat-form-field><mat-label>From date</mat-label><input matInput type="date" [formControl]="dateFrom"></mat-form-field>
      <mat-form-field><mat-label>To date</mat-label><input matInput type="date" [formControl]="dateTo"></mat-form-field>
      <button mat-stroked-button type="button" (click)="reload()">Apply filters</button>
    </section>
    @if(loading()){<div class="cc-loading">Loading reviews…</div>}@else if(!page()?.items?.length){<div class="cc-empty-state"><mat-icon>star_outline</mat-icon><p>No visible reviews yet.</p></div>}
    @else{<section class="cc-card-grid">@for(r of page()!.items;track r.id){<article class="cc-card"><header><strong>{{r.patientDisplayName}}</strong><span>{{r.rating}} ★</span></header><small><mat-icon>verified</mat-icon> Verified interaction · {{r.createdAt|date:'mediumDate'}}</small><p>{{r.comment||'Rating only'}}</p></article>}</section>
    <mat-paginator [length]="page()!.totalCount" [pageSize]="10" (page)="change($event)"/>}
  `,styles:[`.summary{display:grid;gap:8px}.summary div{display:flex;gap:12px;align-items:baseline}.summary strong{font-size:2rem}.summary label{display:grid;grid-template-columns:60px 1fr 30px;gap:10px}progress{width:100%;accent-color:#b77b00}article header,article small{display:flex;justify-content:space-between;align-items:center}article small{justify-content:flex-start;gap:4px;color:#586a67}`],
  changeDetection:ChangeDetectionStrategy.OnPush,
})
export class OwnerReviews implements OnInit{
  private readonly api=inject(ReviewService);readonly ownerPath=input.required<string>();
  protected readonly summary=signal<RatingSummary|null>(null);protected readonly page=signal<ReviewPage|null>(null);protected readonly loading=signal(true);
  protected readonly rating=new FormControl<number|null>(null);protected readonly search=new FormControl('',{nonNullable:true});
  protected readonly dateFrom=new FormControl('',{nonNullable:true});protected readonly dateTo=new FormControl('',{nonNullable:true});private pageNumber=1;
  ngOnInit(){this.api.getOwnerSummary(this.ownerPath()).subscribe({next:s=>this.summary.set(s)});this.load()}
  protected reload(){this.pageNumber=1;this.load()}protected change(e:PageEvent){this.pageNumber=e.pageIndex+1;this.load()}
  protected rows(s:RatingSummary){return[{star:5,count:s.distribution.fiveStars},{star:4,count:s.distribution.fourStars},{star:3,count:s.distribution.threeStars},{star:2,count:s.distribution.twoStars},{star:1,count:s.distribution.oneStar}]}
  private load(){this.loading.set(true);this.api.getOwnerReviews(this.ownerPath(),{page:this.pageNumber,pageSize:10,rating:this.rating.value,search:this.search.value.trim(),dateFrom:this.dateFrom.value,dateTo:this.dateTo.value,sortBy:'newest'}).subscribe({next:p=>{this.page.set(p);this.loading.set(false)},error:()=>this.loading.set(false)})}
}
