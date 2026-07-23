import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ROLE_LABELS, UserRole } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { InsuranceRequestService } from '../../../core/services/insurance-request.service';

interface StatTile {
  label: string;
  value: number | string;
  icon: string;
}

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
      {
        label: 'My appointments',
        description: 'Track your bookings and their status.',
        route: '/dashboard/patient/appointments',
        icon: 'event_note',
      },
      {
        label: 'Insurance requests',
        description: 'Submit and track digital insurance requests.',
        route: '/dashboard/patient/insurance-requests',
        icon: 'fact_check',
      },
    ],
    comingSoon: ['View medical records', 'Blood bank'],
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
      {
        label: 'My availability',
        description: 'Set the hours patients can book you for.',
        route: '/dashboard/doctor/availability',
        icon: 'schedule',
      },
      {
        label: 'Appointments',
        description: 'Review requests and run your schedule.',
        route: '/dashboard/doctor/appointments',
        icon: 'event_note',
      },
    ],
    comingSoon: ['Patient medical history', 'Prescriptions'],
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
      {
        label: 'Appointments',
        description: 'A read-only view of every scheduled visit.',
        route: '/dashboard/hospital/appointments',
        icon: 'event_note',
      },
      {
        label: 'Insurance requests',
        description: 'Review and act on patient insurance requests.',
        route: '/dashboard/hospital/insurance-requests',
        icon: 'fact_check',
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
      {
        label: 'Insurance companies',
        description: 'Manage the insurance company list.',
        route: '/super-admin/insurance-companies',
        icon: 'fact_check',
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
export class RoleDashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly appointments = inject(AppointmentService);
  private readonly insuranceRequests = inject(InsuranceRequestService);

  /** Set from the route data, so one component serves all four role dashboards. */
  readonly role = input.required<UserRole>();

  protected readonly user = this.auth.currentUser;

  protected readonly config = computed(() => DASHBOARDS[this.role()]);
  protected readonly roleLabel = computed(() => ROLE_LABELS[this.role()]);

  protected readonly stats = signal<StatTile[] | null>(null);

  ngOnInit(): void {
    // Scoped to the signed-in profile server-side; this just decides which call to make.
    switch (this.role()) {
      case 'Patient':
        forkJoin({
          appointments: this.appointments.getPatientDashboardStats(),
          insurance: this.insuranceRequests.getPatientDashboardStats(),
        }).subscribe({
          next: ({ appointments: s, insurance: i }) =>
            this.stats.set([
              {
                label: 'Next appointment',
                value: s.nextAppointment
                  ? `${s.nextAppointment.appointmentDate} ${s.nextAppointment.startTime.slice(0, 5)}`
                  : 'None scheduled',
                icon: 'event_upcoming',
              },
              { label: 'Upcoming appointments', value: s.upcomingCount, icon: 'event_available' },
              { label: 'Pending requests', value: s.pendingCount, icon: 'hourglass_top' },
              { label: 'Pending insurance requests', value: i.pendingCount, icon: 'fact_check' },
              { label: 'Approved insurance requests', value: i.approvedCount, icon: 'verified' },
            ]),
          error: () => this.stats.set([]),
        });
        break;

      case 'Doctor':
        this.appointments.getDoctorDashboardStats().subscribe({
          next: (s) =>
            this.stats.set([
              { label: "Today's appointments", value: s.todayCount, icon: 'today' },
              { label: 'Pending requests', value: s.pendingCount, icon: 'hourglass_top' },
              { label: 'Confirmed', value: s.confirmedCount, icon: 'event_available' },
              { label: 'Completed this month', value: s.completedThisMonthCount, icon: 'task_alt' },
            ]),
          error: () => this.stats.set([]),
        });
        break;

      case 'Hospital':
        forkJoin({
          appointments: this.appointments.getHospitalDashboardStats(),
          insurance: this.insuranceRequests.getHospitalDashboardStats(),
        }).subscribe({
          next: ({ appointments: s, insurance: i }) =>
            this.stats.set([
              { label: "Today's appointments", value: s.todayCount, icon: 'today' },
              { label: 'Pending appointments', value: s.pendingCount, icon: 'hourglass_top' },
              { label: 'Active approved doctors', value: s.activeApprovedDoctorsCount, icon: 'groups' },
              { label: 'New insurance requests', value: i.pendingCount, icon: 'fact_check' },
              { label: 'Insurance under review', value: i.underReviewCount, icon: 'pending_actions' },
              { label: 'Approved this month', value: i.approvedThisMonthCount, icon: 'task_alt' },
              { label: 'Rejected this month', value: i.rejectedThisMonthCount, icon: 'block' },
            ]),
          error: () => this.stats.set([]),
        });
        break;

      default:
        this.stats.set([]);
    }
  }
}
