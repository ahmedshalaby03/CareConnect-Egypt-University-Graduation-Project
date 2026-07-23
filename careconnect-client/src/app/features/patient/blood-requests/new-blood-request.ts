import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import {
  friendlyMessageOf,
  validationErrorsOf,
} from '../../../core/interceptors/error.interceptor';
import { BLOOD_GROUPS, BloodGroup } from '../../../core/models/blood-group.model';
import { BloodAvailability } from '../../../core/models/blood-bank.model';
import { BLOOD_REQUEST_URGENCIES, BloodRequestUrgency } from '../../../core/models/blood-request.model';
import { BloodBankService } from '../../../core/services/blood-bank.service';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-new-blood-request',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './new-blood-request.html',
  styleUrl: './new-blood-request.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewBloodRequest implements OnInit {
  private readonly bloodBank = inject(BloodBankService);
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Optional prefill from the blood-bank search page, bound via withComponentInputBinding. */
  readonly hospitalProfileId = input<string>();
  readonly bloodGroup = input<string>();

  protected readonly bloodGroups = BLOOD_GROUPS;
  protected readonly urgencies = BLOOD_REQUEST_URGENCIES;

  protected readonly searchingHospitals = signal(false);
  protected readonly hospitals = signal<BloodAvailability[]>([]);
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly serverErrorList = signal<string[]>([]);

  protected readonly bloodGroupControl = this.fb.control<BloodGroup | ''>('');
  protected readonly hospitalControl = this.fb.control<string>('');

  protected readonly form = this.fb.group({
    unitsRequested: [1, [Validators.required, Validators.min(1), Validators.max(20)]],
    beneficiaryName: ['', [Validators.required, Validators.maxLength(150)]],
    beneficiaryAge: [null as number | null, [Validators.min(0), Validators.max(120)]],
    contactPhoneNumber: ['', [Validators.required, Validators.maxLength(30)]],
    medicalCondition: ['', [Validators.maxLength(500)]],
    hospitalOrFacilityName: ['', [Validators.maxLength(200)]],
    requestNotes: ['', [Validators.maxLength(1000)]],
    urgency: ['Normal' as BloodRequestUrgency, [Validators.required]],
  });

  protected readonly selectedHospital = computed(() => {
    const id = this.hospitalControl.value;
    return this.hospitals().find((h) => h.hospitalProfileId === id) ?? null;
  });

  protected readonly exceedsAvailable = computed(() => {
    const hospital = this.selectedHospital();
    const units = this.form.controls.unitsRequested.value;
    return hospital !== null && units > hospital.availableUnits;
  });

  ngOnInit(): void {
    const prefillGroup = this.bloodGroup() as BloodGroup | undefined;

    if (prefillGroup && this.bloodGroups.includes(prefillGroup)) {
      this.bloodGroupControl.setValue(prefillGroup);
      this.searchHospitals(prefillGroup, this.hospitalProfileId());
    }
  }

  protected onBloodGroupChange(): void {
    this.hospitalControl.setValue('');
    const group = this.bloodGroupControl.value;

    if (group) {
      this.searchHospitals(group, null);
    } else {
      this.hospitals.set([]);
    }
  }

  protected submit(): void {
    this.serverError.set(null);
    this.serverErrorList.set([]);

    const hospital = this.selectedHospital();

    if (!hospital || this.form.invalid) {
      this.form.markAllAsTouched();
      if (!hospital) {
        this.serverError.set('Select a blood group and a hospital first.');
      }
      return;
    }

    if (this.submitting()) {
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);

    this.bloodRequests
      .createRequest({
        hospitalProfileId: hospital.hospitalProfileId,
        bloodGroup: hospital.bloodGroup,
        unitsRequested: raw.unitsRequested,
        beneficiaryName: raw.beneficiaryName.trim(),
        beneficiaryAge: raw.beneficiaryAge,
        contactPhoneNumber: raw.contactPhoneNumber.trim(),
        medicalCondition: raw.medicalCondition.trim() || null,
        hospitalOrFacilityName: raw.hospitalOrFacilityName.trim() || null,
        requestNotes: raw.requestNotes.trim() || null,
        urgency: raw.urgency,
      })
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.notify.success(response.message);
          void this.router.navigate([
            '/dashboard/patient/blood-requests',
            response.data!.bloodRequestId,
          ]);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          // A 409 means a similar active request already exists for this beneficiary.
          this.serverError.set(friendlyMessageOf(error, 'Could not submit the blood request.'));
          this.serverErrorList.set(validationErrorsOf(error));
        },
      });
  }

  private searchHospitals(bloodGroup: BloodGroup, preselectHospitalId: string | null | undefined): void {
    this.searchingHospitals.set(true);

    this.bloodBank
      .searchAvailability({ bloodGroup, availableOnly: true, page: 1, pageSize: 100 })
      .subscribe({
        next: (result) => {
          this.searchingHospitals.set(false);
          this.hospitals.set(result.items);

          if (preselectHospitalId && result.items.some((h) => h.hospitalProfileId === preselectHospitalId)) {
            this.hospitalControl.setValue(preselectHospitalId);
          }
        },
        error: (error: unknown) => {
          this.searchingHospitals.set(false);
          this.notify.error(friendlyMessageOf(error, 'Could not search hospitals.'));
        },
      });
  }
}
