import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  MEDICAL_SERVICE_PROVIDER_TYPES,
  MedicalServiceCategoryOption,
  MedicalServiceProviderSummary,
  PROVIDER_TYPE_LABELS,
} from '../../../core/models/medical-service-provider.model';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { MedicalServiceProviderService } from '../../../core/services/medical-service-provider.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-medical-service-provider-list',
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cc-page">
      <header class="page-head"><div><h1>Medical services</h1><p>Discover published medical centers, laboratories and care providers.</p></div><button mat-stroked-button (click)="nearMe()" [disabled]="locating()"><mat-icon>near_me</mat-icon>{{ locating() ? 'Finding…' : coordinates() ? 'Refresh my location' : 'Near me' }}</button></header>
      <form class="cc-filters filters" [formGroup]="form">
        <mat-form-field><mat-label>Provider or service</mat-label><input matInput formControlName="search"/></mat-form-field>
        <mat-form-field><mat-label>Provider type</mat-label><mat-select formControlName="providerType"><mat-option [value]="null">All types</mat-option>@for (type of providerTypes; track type) { <mat-option [value]="type">{{ labels[type] }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>Category</mat-label><mat-select formControlName="categoryId"><mat-option [value]="null">All categories</mat-option>@for (item of categories(); track item.id) { <mat-option [value]="item.id">{{ item.name }}</mat-option> }</mat-select></mat-form-field>
        <mat-form-field><mat-label>Governorate</mat-label><input matInput formControlName="governorate"/></mat-form-field>
        <mat-form-field><mat-label>City</mat-label><input matInput formControlName="city"/></mat-form-field>
        <mat-form-field><mat-label>Sort by</mat-label><mat-select formControlName="sortBy"><mat-option value="name">Name</mat-option><mat-option value="minimumPrice">Minimum price</mat-option><mat-option value="distance" [disabled]="!coordinates()">Distance</mat-option></mat-select></mat-form-field>
        @if (coordinates()) { <mat-form-field><mat-label>Radius</mat-label><mat-select formControlName="radiusKm">@for (radius of radii; track radius) { <mat-option [value]="radius">{{ radius }} km</mat-option> }</mat-select></mat-form-field> }
      </form>
      @if (coordinates()) { <div class="cc-notice"><mat-icon>privacy_tip</mat-icon>Your one-time location is used only for this search and is not saved. Distances are approximate straight-line values.</div> }
      @if (loading()) { <mat-progress-bar mode="indeterminate"/> }
      @if (error()) { <div class="cc-notice cc-notice--error">{{ error() }}</div> }
      <div class="cc-card-grid provider-grid">
        @for (provider of providers(); track provider.id) {
          <article class="cc-card provider-card"><div><span class="eyebrow">{{ labels[provider.providerType] }}</span><h2>{{ provider.businessName }}</h2><p><mat-icon>location_on</mat-icon>{{ provider.city }}, {{ provider.governorate }} @if (provider.distanceKm !== null) { · {{ provider.distanceKm }} km away }</p></div>
            <p>{{ provider.description }}</p><div class="chips">@for (category of provider.categories; track category.id) { <span class="cc-role-chip">{{ category.name }}</span> }</div>
            <footer><span>@if (provider.minimumServicePrice !== null) { From <strong>{{ provider.minimumServicePrice | currency:'EGP ':'symbol':'1.0-2' }}</strong> }</span><a mat-flat-button [routerLink]="['/medical-service-providers', provider.id]">View details</a></footer>
          </article>
        } @empty { @if (!loading()) { <div class="cc-empty-state"><mat-icon>search_off</mat-icon><h2>No providers found</h2><p>Try broader filters or a larger nearby radius.</p></div> } }
      </div>
      <mat-paginator [length]="totalCount()" [pageIndex]="pageIndex()" [pageSize]="pageSize()" [pageSizeOptions]="[6,12,24]" (page)="onPage($event)"/>
    </section>
  `,
  styles: `.page-head,.provider-card footer{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;flex-wrap:wrap}.filters{grid-template-columns:repeat(3,minmax(0,1fr));margin:20px 0}.cc-notice{display:flex;gap:10px;align-items:center;margin-bottom:16px}.provider-grid{margin:20px 0}.provider-card{display:flex;flex-direction:column;gap:10px}.provider-card h2{margin:4px 0}.provider-card p{color:var(--mat-sys-on-surface-variant)}.provider-card p mat-icon{font-size:18px;width:18px;height:18px;vertical-align:middle}.eyebrow{color:var(--cc-primary);font-weight:700}.chips{display:flex;gap:7px;flex-wrap:wrap}.provider-card footer{align-items:center;margin-top:auto;padding-top:12px}@media(max-width:850px){.filters{grid-template-columns:1fr 1fr}}@media(max-width:600px){.filters{grid-template-columns:1fr}}`,
})
export class MedicalServiceProviderListPage implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly service = inject(MedicalServiceProviderService);
  private readonly geolocation = inject(GeolocationService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly providerTypes = MEDICAL_SERVICE_PROVIDER_TYPES;
  protected readonly labels = PROVIDER_TYPE_LABELS;
  protected readonly radii = [5, 10, 25, 50, 100];
  protected readonly providers = signal<MedicalServiceProviderSummary[]>([]);
  protected readonly categories = signal<MedicalServiceCategoryOption[]>([]);
  protected readonly coordinates = signal<{ latitude: number; longitude: number } | null>(null);
  protected readonly loading = signal(false);
  protected readonly locating = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(6);
  protected readonly form = this.fb.group({
    search: [''], providerType: [null as typeof MEDICAL_SERVICE_PROVIDER_TYPES[number] | null],
    categoryId: [null as string | null], governorate: [''], city: [''],
    sortBy: ['name' as 'name' | 'distance' | 'minimumPrice'], radiusKm: [25],
  });
  ngOnInit(): void {
    this.service.getActiveCategories().subscribe((items) => this.categories.set(items));
    this.form.valueChanges.pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => { this.pageIndex.set(0); this.load(); });
    this.load();
  }
  protected onPage(event: PageEvent): void { this.pageIndex.set(event.pageIndex); this.pageSize.set(event.pageSize); this.load(); }
  protected nearMe(): void {
    this.locating.set(true);
    this.geolocation.getCurrentPosition().then((coords) => { this.coordinates.set(coords); this.locating.set(false); this.form.controls.sortBy.setValue('distance'); this.load(); this.notify.success('Nearby providers are sorted by approximate distance.'); }).catch((error: unknown) => { this.locating.set(false); this.notify.error(error instanceof GeolocationFailure ? `${error.message} Use Governorate and City instead.` : 'Location is unavailable. Use Governorate and City instead.'); });
  }
  private load(): void {
    this.loading.set(true); this.error.set(null);
    const value = this.form.getRawValue(); const coords = this.coordinates();
    this.service.search({ ...value, latitude: coords?.latitude ?? null, longitude: coords?.longitude ?? null, page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe({
      next: (result) => { this.providers.set(result.items); this.totalCount.set(result.totalCount); this.loading.set(false); },
      error: (error: unknown) => { this.loading.set(false); this.error.set(friendlyMessageOf(error, 'Could not load medical service providers.')); },
    });
  }
}
