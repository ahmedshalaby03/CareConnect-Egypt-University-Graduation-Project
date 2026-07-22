import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { AffiliationStatus, DoctorHospitalRequest } from '../../../core/models/affiliation.model';
import { EGYPT_GOVERNORATES, HospitalDirectoryItem } from '../../../core/models/directory.model';
import { DoctorProfile } from '../../../core/models/doctor.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AffiliationService } from '../../../core/services/affiliation.service';
import { DirectoryService } from '../../../core/services/directory.service';
import { DoctorProfileService } from '../../../core/services/doctor-profile.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';

@Component({
  selector: 'app-doctor-hospitals',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './doctor-hospitals.html',
  styleUrl: './doctor-hospitals.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorHospitals implements OnInit {
  private readonly directory = inject(DirectoryService);
  private readonly affiliations = inject(AffiliationService);
  private readonly profiles = inject(DoctorProfileService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly governorates = EGYPT_GOVERNORATES;

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly governorateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly hospitals = signal<HospitalDirectoryItem[]>([]);
  protected readonly profile = signal<DoctorProfile | null>(null);
  protected readonly loading = signal(true);
  protected readonly sendingFor = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(9);

  /**
   * Existing relationships keyed by hospital id, so a card can show "pending" or "working
   * here" instead of offering a request that the API would reject anyway.
   */
  private readonly relationships = signal<Map<string, AffiliationStatus>>(new Map());

  protected readonly canRequest = computed(() => this.profile()?.isProfileCompleted ?? false);

  ngOnInit(): void {
    forkJoin({
      specialties: this.specialtyService.getActive(),
      profile: this.profiles.get(),
    }).subscribe({
      next: ({ specialties, profile }) => {
        this.specialties.set(specialties);
        this.profile.set(profile);
      },
      error: (error: unknown) =>
        this.notify.error(friendlyMessageOf(error, 'Could not load your profile.')),
    });

    this.loadRelationships();

    this.searchControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.resetAndLoad());

    this.cityControl.valueChanges
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.resetAndLoad());

    this.load();
  }

  protected onFilterChange(): void {
    this.resetAndLoad();
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.governorateControl.setValue('', { emitEvent: false });
    this.cityControl.setValue('', { emitEvent: false });
    this.specialtyControl.setValue('', { emitEvent: false });
    this.resetAndLoad();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected statusFor(hospitalId: string): AffiliationStatus | null {
    return this.relationships().get(hospitalId) ?? null;
  }

  /** True when a live relationship already exists, matching the API's duplicate rule. */
  protected isBlocked(hospitalId: string): boolean {
    const status = this.statusFor(hospitalId);
    return status === 'Pending' || status === 'Approved';
  }

  protected offersMySpecialty(hospital: HospitalDirectoryItem): boolean {
    const mySpecialtyId = this.profile()?.specialty?.id;
    return !!mySpecialtyId && hospital.specialties.some((s) => s.id === mySpecialtyId);
  }

  protected requestAffiliation(hospital: HospitalDirectoryItem): void {
    if (this.sendingFor() || this.isBlocked(hospital.id)) {
      return;
    }

    this.sendingFor.set(hospital.id);

    this.affiliations.requestAffiliation(hospital.id).subscribe({
      next: (response) => {
        this.sendingFor.set(null);
        this.notify.success(response.message);

        this.relationships.update((map) => {
          const next = new Map(map);
          next.set(hospital.id, 'Pending');
          return next;
        });
      },
      error: (error: unknown) => {
        this.sendingFor.set(null);
        // The backend is the real gate: a specialty mismatch or duplicate lands here.
        this.notify.error(friendlyMessageOf(error, 'Could not send the request.'));
        this.loadRelationships();
      },
    });
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.load();
  }

  private load(): void {
    this.loading.set(true);

    this.directory
      .searchHospitals({
        search: this.searchControl.value,
        governorate: this.governorateControl.value || null,
        city: this.cityControl.value,
        specialtyId: this.specialtyControl.value || null,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.loading.set(false);
          this.hospitals.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not load hospitals.'));
        },
      });
  }

  private loadRelationships(): void {
    // Page size is generous: a doctor's request history is small, and one call keeps the
    // card states accurate without a lookup per hospital.
    this.affiliations.getDoctorRequests({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.relationships.set(this.toStatusMap(result.items)),
      error: () => undefined,
    });
  }

  private toStatusMap(requests: DoctorHospitalRequest[]): Map<string, AffiliationStatus> {
    const map = new Map<string, AffiliationStatus>();

    // Newest first from the API, so the first entry per hospital is the current one.
    for (const request of requests) {
      if (!map.has(request.hospitalProfileId)) {
        map.set(request.hospitalProfileId, request.statusName);
      }
    }

    return map;
  }
}
