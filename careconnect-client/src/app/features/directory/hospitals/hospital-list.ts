import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { BLOOD_GROUPS } from '../../../core/models/blood-group.model';
import { EGYPT_GOVERNORATES, HospitalDirectoryItem } from '../../../core/models/directory.model';
import { DEFAULT_NEARBY_RADIUS_KM, NEARBY_RADIUS_OPTIONS_KM } from '../../../core/models/hospital-discovery.model';
import { SpecialtyOption } from '../../../core/models/specialty.model';
import { AuthService } from '../../../core/services/auth.service';
import { DirectoryService } from '../../../core/services/directory.service';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { HospitalDiscoveryService } from '../../../core/services/hospital-discovery.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SpecialtyService } from '../../../core/services/specialty.service';

type SearchMode = 'all' | 'nearby';

/** Public hospital directory. Open to every signed-in role, extended with nearby/distance search. */
@Component({
  selector: 'app-hospital-list',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './hospital-list.html',
  styleUrl: './hospital-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalList implements OnInit {
  private readonly directory = inject(DirectoryService);
  private readonly discovery = inject(HospitalDiscoveryService);
  private readonly specialtyService = inject(SpecialtyService);
  private readonly geolocation = inject(GeolocationService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly governorates = EGYPT_GOVERNORATES;
  protected readonly bloodGroups = BLOOD_GROUPS;
  protected readonly radiusOptions = NEARBY_RADIUS_OPTIONS_KM;
  protected readonly isPatient = computed(() => this.auth.role() === 'Patient');

  protected readonly mode = signal<SearchMode>('all');
  protected readonly locatingDevice = signal(false);
  protected readonly coordinates = signal<{ latitude: number; longitude: number } | null>(null);
  protected readonly locationDeniedMessage = signal<string | null>(null);

  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly governorateControl = new FormControl<string>('', { nonNullable: true });
  protected readonly cityControl = new FormControl<string>('', { nonNullable: true });
  protected readonly specialtyControl = new FormControl<string>('', { nonNullable: true });
  protected readonly bloodGroupControl = new FormControl<string>('', { nonNullable: true });
  protected readonly hasAvailableAppointmentsControl = new FormControl<boolean>(false, { nonNullable: true });
  protected readonly hasAvailableBloodControl = new FormControl<boolean>(false, { nonNullable: true });
  protected readonly radiusControl = new FormControl<number>(DEFAULT_NEARBY_RADIUS_KM, { nonNullable: true });

  protected readonly specialties = signal<SpecialtyOption[]>([]);
  protected readonly hospitals = signal<HospitalDirectoryItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(9);

  ngOnInit(): void {
    this.specialtyService.getActive().subscribe({
      next: (items) => this.specialties.set(items),
      error: () => undefined,
    });

    for (const control of [this.searchControl, this.cityControl]) {
      control.valueChanges
        .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.resetAndLoad());
    }

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
    this.bloodGroupControl.setValue('', { emitEvent: false });
    this.hasAvailableAppointmentsControl.setValue(false, { emitEvent: false });
    this.hasAvailableBloodControl.setValue(false, { emitEvent: false });
    this.resetAndLoad();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  /**
   * The one and only trigger for browser geolocation on this page - never called
   * automatically. Coordinates are used for this search request only and are never sent
   * anywhere except the nearby-search call itself.
   */
  protected findNearMe(): void {
    this.locationDeniedMessage.set(null);
    this.locatingDevice.set(true);

    this.geolocation
      .getCurrentPosition()
      .then((coords) => {
        this.locatingDevice.set(false);
        this.coordinates.set(coords);
        this.mode.set('nearby');
        this.resetAndLoad();
      })
      .catch((error: unknown) => {
        this.locatingDevice.set(false);

        if (error instanceof GeolocationFailure) {
          const messages: Record<typeof error.reason, string> = {
            denied:
              'Location permission was denied. You can still search by governorate and city below.',
            unavailable: 'Your location could not be determined. You can search by governorate and city below.',
            timeout: 'Getting your location took too long. You can try again or search by governorate and city.',
          };
          this.locationDeniedMessage.set(messages[error.reason]);
          return;
        }

        this.locationDeniedMessage.set('Your location could not be determined.');
      });
  }

  protected backToAllHospitals(): void {
    this.mode.set('all');
    this.resetAndLoad();
  }

  private resetAndLoad(): void {
    this.pageIndex.set(0);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set(null);

    const bloodGroup = (this.bloodGroupControl.value || null) as never;
    const hasAvailableAppointments = this.hasAvailableAppointmentsControl.value || null;
    const hasAvailableBlood = this.hasAvailableBloodControl.value || null;

    const request$ =
      this.mode() === 'nearby' && this.coordinates()
        ? this.discovery.searchNearby({
            latitude: this.coordinates()!.latitude,
            longitude: this.coordinates()!.longitude,
            radiusKm: this.radiusControl.value,
            specialtyId: this.specialtyControl.value || null,
            governorate: this.governorateControl.value || null,
            city: this.cityControl.value || null,
            searchTerm: this.searchControl.value,
            hasAvailableAppointments,
            hasAvailableBlood,
            bloodGroup,
            page: this.pageIndex() + 1,
            pageSize: this.pageSize(),
          })
        : this.directory.searchHospitals({
            search: this.searchControl.value,
            governorate: this.governorateControl.value || null,
            city: this.cityControl.value,
            specialtyId: this.specialtyControl.value || null,
            hasAvailableAppointments,
            hasAvailableBlood,
            bloodGroup,
            page: this.pageIndex() + 1,
            pageSize: this.pageSize(),
          });

    request$.subscribe({
      next: (result) => {
        this.loading.set(false);
        this.hospitals.set(result.items);
        this.totalCount.set(result.totalCount);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        const message = friendlyMessageOf(error, 'Could not load hospitals.');
        this.loadError.set(message);
        this.notify.error(message);
      },
    });
  }
}
