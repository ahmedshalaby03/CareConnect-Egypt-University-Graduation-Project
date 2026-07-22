import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { HospitalDirectoryDetails } from '../../../core/models/directory.model';
import { DirectoryService } from '../../../core/services/directory.service';

/** Public hospital page: profile, specialties and the doctors approved to work there. */
@Component({
  selector: 'app-hospital-details',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './hospital-details.html',
  styleUrl: './hospital-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HospitalDetails implements OnInit {
  private readonly directory = inject(DirectoryService);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly hospital = signal<HospitalDirectoryDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.directory.getHospital(this.id()).subscribe({
      next: (hospital) => {
        this.hospital.set(hospital);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this hospital.'));
      },
    });
  }
}
