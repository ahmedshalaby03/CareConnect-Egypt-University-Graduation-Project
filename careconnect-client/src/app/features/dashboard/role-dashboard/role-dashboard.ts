import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { ROLE_LABELS, UserRole } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';

interface QuickLink {
  label: string;
  description: string;
  route: string;
  icon: string;
}

interface DashboardConfig {
  accent: string;
  icon: string;
  intro: string;
  quickLinks: QuickLink[];
  comingSoon: string[];
}

// Placeholder content per role. Real features (appointments, insurance, blood bank, maps,
// AI) are explicitly out of scope for this step and land in later ones.
const DASHBOARDS: Record<UserRole, DashboardConfig> = {
  Patient: {
    accent: '#00796b',
    icon: 'personal_injury',
    intro: 'Manage your health journey with CareConnect Egypt.',
    quickLinks: [
      {
        label: 'Find a doctor',
        description: 'Search by specialty and location.',
        route: '/doctors',
        icon: 'medical_information',
      },
      {
        label: 'Browse hospitals',
        description: 'See hospitals and the doctors who work there.',
        route: '/hospitals',
        icon: 'local_hospital',
      },
    ],
    comingSoon: ['Book appointments', 'View medical records', 'Insurance requests'],
  },
  Doctor: {
    accent: '#00695c',
    icon: 'medical_information',
    intro: 'Manage your practice and connect with hospitals.',
    quickLinks: [
      {
        label: 'My profile',
        description: 'Keep your specialty and credentials up to date.',
        route: '/dashboard/doctor/profile',
        icon: 'badge',
      },
      {
        label: 'Find hospitals',
        description: 'Apply to join a hospital medical team.',
        route: '/dashboard/doctor/hospitals',
        icon: 'travel_explore',
      },
      {
        label: 'My requests',
        description: 'Track applications and set your primary hospital.',
        route: '/dashboard/doctor/hospital-requests',
        icon: 'assignment',
      },
    ],
    comingSoon: ['Manage availability', 'Consultation requests', 'Patient history'],
  },
  Hospital: {
    accent: '#0277bd',
    icon: 'local_hospital',
    intro: 'Represent your hospital across the CareConnect network.',
    quickLinks: [
      {
        label: 'Hospital profile',
        description: 'Update your details and the specialties you offer.',
        route: '/dashboard/hospital/profile',
        icon: 'domain',
      },
      {
        label: 'Doctor requests',
        description: 'Approve or decline doctors applying to join.',
        route: '/dashboard/hospital/doctor-requests',
        icon: 'how_to_reg',
      },
      {
        label: 'Our doctors',
        description: 'Manage the doctors on your medical team.',
        route: '/dashboard/hospital/doctors',
        icon: 'groups',
      },
    ],
    comingSoon: ['Departments & staff', 'Bed availability', 'Blood bank'],
  },
  MedicalServiceProvider: {
    accent: '#5e35b1',
    icon: 'medical_services',
    intro: 'Offer your medical services to patients across Egypt.',
    quickLinks: [
      {
        label: 'Browse doctors',
        description: 'See who is practising on the network.',
        route: '/doctors',
        icon: 'medical_information',
      },
      {
        label: 'Browse hospitals',
        description: 'Explore hospitals across Egypt.',
        route: '/hospitals',
        icon: 'local_hospital',
      },
    ],
    comingSoon: ['Service catalogue', 'Incoming requests', 'Coverage map'],
  },
  SuperAdmin: {
    accent: '#c62828',
    icon: 'admin_panel_settings',
    intro: 'Platform administration.',
    quickLinks: [
      {
        label: 'Users',
        description: 'Search accounts and activate or deactivate them.',
        route: '/super-admin',
        icon: 'manage_accounts',
      },
      {
        label: 'Specialties',
        description: 'Manage the medical specialty list.',
        route: '/super-admin/specialties',
        icon: 'category',
      },
    ],
    comingSoon: [],
  },
};

@Component({
  selector: 'app-role-dashboard',
  imports: [DatePipe, RouterLink, MatIconModule],
  templateUrl: './role-dashboard.html',
  styleUrl: './role-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleDashboard {
  private readonly auth = inject(AuthService);

  /** Set from the route data, so one component serves all four role dashboards. */
  readonly role = input.required<UserRole>();

  protected readonly user = this.auth.currentUser;

  protected readonly config = computed(() => DASHBOARDS[this.role()]);
  protected readonly roleLabel = computed(() => ROLE_LABELS[this.role()]);
}
