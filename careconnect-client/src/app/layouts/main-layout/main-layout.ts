import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ROLE_LABELS, UserRole } from '../../core/models/user.model';
import { AuthService } from '../../core/services/auth.service';

interface NavLink {
  label: string;
  route: string;
  icon: string;
  /** Match the whole URL rather than a prefix, for links that are a parent of others. */
  exact?: boolean;
}

/** Everything a role can reach from the top bar. The API authorises each call regardless. */
const NAV_BY_ROLE: Record<UserRole, NavLink[]> = {
  Patient: [
    { label: 'Dashboard', route: '/dashboard/patient', icon: 'dashboard', exact: true },
    { label: 'Doctors', route: '/doctors', icon: 'medical_information' },
    { label: 'Hospitals', route: '/hospitals', icon: 'local_hospital' },
    { label: 'My appointments', route: '/dashboard/patient/appointments', icon: 'event_note' },
    { label: 'Insurance requests', route: '/dashboard/patient/insurance-requests', icon: 'fact_check' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
    { label: 'Blood requests', route: '/dashboard/patient/blood-requests', icon: 'water_drop' },
  ],
  Doctor: [
    { label: 'Dashboard', route: '/dashboard/doctor', icon: 'dashboard', exact: true },
    { label: 'My profile', route: '/dashboard/doctor/profile', icon: 'badge' },
    { label: 'Find hospitals', route: '/dashboard/doctor/hospitals', icon: 'travel_explore' },
    { label: 'My requests', route: '/dashboard/doctor/hospital-requests', icon: 'assignment' },
    { label: 'Appointments', route: '/dashboard/doctor/appointments', icon: 'event_note' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
  ],
  Hospital: [
    { label: 'Dashboard', route: '/dashboard/hospital', icon: 'dashboard', exact: true },
    { label: 'Profile', route: '/dashboard/hospital/profile', icon: 'domain' },
    { label: 'Location', route: '/dashboard/hospital/location', icon: 'near_me' },
    { label: 'Requests', route: '/dashboard/hospital/doctor-requests', icon: 'how_to_reg' },
    { label: 'Our doctors', route: '/dashboard/hospital/doctors', icon: 'groups' },
    { label: 'Appointments', route: '/dashboard/hospital/appointments', icon: 'event_note' },
    { label: 'Insurance requests', route: '/dashboard/hospital/insurance-requests', icon: 'fact_check' },
    { label: 'Blood stock', route: '/dashboard/hospital/blood-stock', icon: 'bloodtype' },
    { label: 'Blood requests', route: '/dashboard/hospital/blood-requests', icon: 'water_drop' },
  ],
  MedicalServiceProvider: [
    { label: 'Dashboard', route: '/dashboard/service-provider', icon: 'dashboard', exact: true },
    { label: 'Doctors', route: '/doctors', icon: 'medical_information' },
    { label: 'Hospitals', route: '/hospitals', icon: 'local_hospital' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
  ],
  SuperAdmin: [
    { label: 'Users', route: '/super-admin', icon: 'manage_accounts', exact: true },
    { label: 'Specialties', route: '/super-admin/specialties', icon: 'category' },
    { label: 'Insurance companies', route: '/super-admin/insurance-companies', icon: 'fact_check' },
    { label: 'Doctors', route: '/doctors', icon: 'medical_information' },
    { label: 'Hospitals', route: '/hospitals', icon: 'local_hospital' },
    { label: 'Blood bank', route: '/blood-bank', icon: 'bloodtype' },
  ],
};

/** Application chrome for every signed-in screen: brand bar, navigation, account menu. */
@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayout {
  private readonly auth = inject(AuthService);

  protected readonly user = this.auth.currentUser;

  protected readonly navLinks = computed<NavLink[]>(() => {
    const role = this.user()?.role;
    return role ? (NAV_BY_ROLE[role] ?? []) : [];
  });

  protected readonly roleLabel = computed(() => {
    const role = this.user()?.role;
    return role ? ROLE_LABELS[role] : '';
  });

  protected readonly initials = computed(() => {
    const name = this.user()?.fullName?.trim();
    if (!name) {
      return '?';
    }

    const parts = name.split(/\s+/).filter(Boolean);
    const letters = parts.length > 1 ? `${parts[0][0]}${parts[parts.length - 1][0]}` : parts[0][0];

    return letters.toUpperCase();
  });

  protected logout(): void {
    this.auth.logout();
  }
}
