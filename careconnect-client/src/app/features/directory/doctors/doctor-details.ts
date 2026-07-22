import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import { DoctorDirectoryDetails } from '../../../core/models/directory.model';
import { DirectoryService } from '../../../core/services/directory.service';

/** Public doctor page: professional details and the hospitals they are approved at. */
@Component({
  selector: 'app-doctor-details',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './doctor-details.html',
  styleUrl: './doctor-details.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorDetails implements OnInit {
  private readonly directory = inject(DirectoryService);

  /** Bound from the route parameter through withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly doctor = signal<DoctorDirectoryDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.directory.getDoctor(this.id()).subscribe({
      next: (doctor) => {
        this.doctor.set(doctor);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(friendlyMessageOf(error, 'Could not load this doctor.'));
      },
    });
  }

  protected initials(fullName: string): string {
    const parts = fullName.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) {
      return '?';
    }

    const letters =
      parts.length > 1 ? `${parts[0][0]}${parts[parts.length - 1][0]}` : parts[0][0];

    return letters.toUpperCase();
  }
}
