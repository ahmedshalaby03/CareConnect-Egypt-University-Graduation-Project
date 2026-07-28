import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { friendlyMessageOf } from '../../../core/interceptors/error.interceptor';
import {
  DashboardAppointmentItem,
  DashboardRequestItem,
  RecentRegistration,
} from '../../../core/models/dashboard.model';
import { ROLE_LABELS, UserRole } from '../../../core/models/user.model';
import { HospitalDirectoryItem } from '../../../core/models/directory.model';
import { AuthService } from '../../../core/services/auth.service';
import { DashboardService } from '../../../core/services/dashboard.service';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { HospitalDiscoveryService } from '../../../core/services/hospital-discovery.service';

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
        label: 'Medical services',
        description: 'Browse providers and request a medical service.',
        route: '/medical-service-providers',
        icon: 'health_and_safety',
      },
      {
        label: 'AI Medical Assistant',
        description: 'Get general health guidance with safety boundaries.',
        route: '/dashboard/patient/ai-assistant',
        icon: 'smart_toy',
      },
      {
        label: 'Insurance requests',
        description: 'Submit and track digital insurance requests.',
        route: '/dashboard/patient/insurance-requests',
        icon: 'fact_check',
      },
      {
        label: 'Blood bank',
        description: 'Search hospitals for the blood group you need.',
        route: '/blood-bank',
        icon: 'bloodtype',
      },
      {
        label: 'Blood requests',
        description: 'Track the blood requests you have submitted.',
        route: '/dashboard/patient/blood-requests',
        icon: 'water_drop',
      },
      {
        label: 'Find nearby hospitals',
        description: 'Search hospitals close to your current location.',
        route: '/hospitals',
        icon: 'near_me',
      },
    ],
    comingSoon: [],
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
      {
        label: 'Unavailable periods',
        description: 'Block future periods when you cannot receive bookings.',
        route: '/dashboard/doctor/unavailable-periods',
        icon: 'event_busy',
      },
      {
        label: 'Patient reviews',
        description: 'See verified feedback from completed appointments.',
        route: '/dashboard/doctor/reviews',
        icon: 'star',
      },
    ],
    comingSoon: [],
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
      {
        label: 'Blood stock',
        description: 'Keep your available blood units up to date.',
        route: '/dashboard/hospital/blood-stock',
        icon: 'bloodtype',
      },
      {
        label: 'Blood requests',
        description: 'Review and act on patient blood requests.',
        route: '/dashboard/hospital/blood-requests',
        icon: 'water_drop',
      },
      {
        label: 'Location',
        description: 'Set your address and map coordinates for nearby search.',
        route: '/dashboard/hospital/location',
        icon: 'near_me',
      },
      {
        label: 'Patient reviews',
        description: 'See verified feedback for completed appointments.',
        route: '/dashboard/hospital/reviews',
        icon: 'star',
      },
    ],
    comingSoon: [],
  },
  MedicalServiceProvider: {
    accent: '#5e35b1',
    icon: 'medical_services',
    intro: 'Offer your medical services to patients across Egypt.',
    quickLinks: [
      {
        label: 'Business profile',
        description: 'Keep your public business details current.',
        route: '/dashboard/service-provider/profile',
        icon: 'storefront',
      },
      {
        label: 'My services',
        description: 'Manage your active and inactive service catalogue.',
        route: '/dashboard/service-provider/services',
        icon: 'medical_services',
      },
      {
        label: 'Working hours',
        description: 'Maintain the hours patients can request.',
        route: '/dashboard/service-provider/working-hours',
        icon: 'schedule',
      },
      {
        label: 'Service requests',
        description: 'Review and manage incoming patient requests.',
        route: '/dashboard/service-provider/requests',
        icon: 'assignment',
      },
      {
        label: 'Public preview',
        description: 'Preview the profile patients can discover.',
        route: '/dashboard/service-provider/preview',
        icon: 'preview',
      },
      {
        label: 'Patient reviews',
        description: 'Read verified patient feedback.',
        route: '/dashboard/service-provider/reviews',
        icon: 'star',
      },
    ],
    comingSoon: [],
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
      {
        label: 'Medical service categories',
        description: 'Manage categories used by service providers.',
        route: '/super-admin/medical-service-categories',
        icon: 'medical_services',
      },
      {
        label: 'Review moderation',
        description: 'Moderate visible and hidden verified reviews.',
        route: '/super-admin/reviews',
        icon: 'policy',
      },
    ],
    comingSoon: [],
  },
};

@Component({
  selector: 'app-role-dashboard',
  imports: [DatePipe, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './role-dashboard.html',
  styleUrl: './role-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleDashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly dashboard = inject(DashboardService);
  private readonly hospitalDiscovery = inject(HospitalDiscoveryService);
  private readonly geolocation = inject(GeolocationService);

  /** Set from the route data, so one component serves all four role dashboards. */
  readonly role = input.required<UserRole>();

  protected readonly user = this.auth.currentUser;

  protected readonly config = computed(() => DASHBOARDS[this.role()]);
  protected readonly roleLabel = computed(() => ROLE_LABELS[this.role()]);

  protected readonly stats = signal<StatTile[] | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly recentRequests = signal<DashboardRequestItem[]>([]);
  protected readonly recentAppointments = signal<DashboardAppointmentItem[]>([]);
  protected readonly recentRegistrations = signal<RecentRegistration[]>([]);

  /**
   * Session-only: filled in solely when the Patient clicks the button below, never on load.
   * The coordinates behind it are used for one search request and are not kept anywhere.
   */
  protected readonly closestHospital = signal<HospitalDirectoryItem | null>(null);
  protected readonly findingClosestHospital = signal(false);
  protected readonly closestHospitalMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDashboard();
  }

  protected loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);
    this.stats.set(null);
    this.recentRequests.set([]);
    this.recentAppointments.set([]);
    this.recentRegistrations.set([]);

    switch (this.role()) {
      case 'Patient':
        this.dashboard.getPatient().subscribe({
          next: (summary) => {
            this.stats.set([
              {
                label: 'Next appointment',
                value: summary.nextAppointment
                  ? `${summary.nextAppointment.appointmentDate} ${summary.nextAppointment.startTime.slice(0, 5)}`
                  : 'None scheduled',
                icon: 'event_upcoming',
              },
              { label: 'Upcoming appointments', value: summary.upcomingAppointmentsCount, icon: 'event_available' },
              { label: 'Pending appointments', value: summary.pendingAppointmentsCount, icon: 'hourglass_top' },
              { label: 'Pending insurance', value: summary.pendingInsuranceRequestsCount, icon: 'fact_check' },
              { label: 'Pending blood requests', value: summary.pendingBloodRequestsCount, icon: 'bloodtype' },
              { label: 'Active service requests', value: summary.activeMedicalServiceRequestsCount, icon: 'medical_services' },
              { label: 'Unread notifications', value: summary.unreadNotificationsCount, icon: 'notifications' },
              { label: 'Reviews available', value: summary.eligibleReviewsCount, icon: 'rate_review' },
            ]);
            this.recentRequests.set(summary.recentRequests);
            this.loading.set(false);
          },
          error: (error: unknown) => this.handleLoadError(error),
        });
        break;

      case 'Doctor':
        this.dashboard.getDoctor().subscribe({
          next: (summary) => {
            this.stats.set([
              { label: "Today's appointments", value: summary.todayAppointmentsCount, icon: 'today' },
              { label: 'Upcoming confirmed', value: summary.upcomingConfirmedAppointmentsCount, icon: 'event_available' },
              { label: 'Pending appointments', value: summary.pendingAppointmentRequestsCount, icon: 'hourglass_top' },
              { label: 'Completed appointments', value: summary.completedAppointmentsCount, icon: 'task_alt' },
              { label: 'Current hospitals', value: summary.currentHospitalAffiliationsCount, icon: 'local_hospital' },
              { label: 'Pending affiliations', value: summary.pendingHospitalAffiliationRequestsCount, icon: 'handshake' },
              { label: 'Average rating', value: summary.averageVisibleRating ?? '—', icon: 'star' },
              { label: 'Visible reviews', value: summary.visibleReviewsCount, icon: 'rate_review' },
              { label: 'Unread notifications', value: summary.unreadNotificationsCount, icon: 'notifications' },
            ]);
            this.recentAppointments.set(summary.recentAppointments);
            this.loading.set(false);
          },
          error: (error: unknown) => this.handleLoadError(error),
        });
        break;

      case 'Hospital':
        this.dashboard.getHospital().subscribe({
          next: (summary) => {
            this.stats.set([
              { label: 'Active doctors', value: summary.activeAffiliatedDoctorsCount, icon: 'groups' },
              { label: 'Pending doctor requests', value: summary.pendingDoctorAffiliationRequestsCount, icon: 'how_to_reg' },
              { label: "Today's appointments", value: summary.todayAppointmentsCount, icon: 'today' },
              { label: 'Pending insurance', value: summary.pendingInsuranceRequestsCount, icon: 'fact_check' },
              { label: 'Pending blood requests', value: summary.pendingBloodRequestsCount, icon: 'water_drop' },
              { label: 'Low blood-stock groups', value: summary.lowBloodStockGroupsCount, icon: 'warning' },
              { label: 'Average rating', value: summary.averageVisibleRating ?? '—', icon: 'star' },
              { label: 'Visible reviews', value: summary.visibleReviewsCount, icon: 'rate_review' },
              { label: 'Unread notifications', value: summary.unreadNotificationsCount, icon: 'notifications' },
            ]);
            this.loading.set(false);
          },
          error: (error: unknown) => this.handleLoadError(error),
        });
        break;

      case 'MedicalServiceProvider':
        this.dashboard.getMedicalServiceProvider().subscribe({
          next: (summary) => {
            this.stats.set([
              { label: 'Publication status', value: summary.isPublished ? 'Published' : 'Draft', icon: 'public' },
              { label: 'Active services', value: summary.activeServicesCount, icon: 'medical_services' },
              { label: 'Inactive services', value: summary.inactiveServicesCount, icon: 'visibility_off' },
              { label: 'Pending requests', value: summary.pendingRequestsCount, icon: 'hourglass_top' },
              { label: 'Accepted upcoming', value: summary.acceptedUpcomingRequestsCount, icon: 'event_available' },
              { label: 'Completed requests', value: summary.completedRequestsCount, icon: 'task_alt' },
              { label: 'Average rating', value: summary.averageVisibleRating ?? '—', icon: 'star' },
              { label: 'Visible reviews', value: summary.visibleReviewsCount, icon: 'rate_review' },
              { label: 'Unread notifications', value: summary.unreadNotificationsCount, icon: 'notifications' },
            ]);
            this.recentRequests.set(summary.upcomingRequests);
            this.loading.set(false);
          },
          error: (error: unknown) => this.handleLoadError(error),
        });
        break;

      case 'SuperAdmin':
        this.dashboard.getSuperAdmin().subscribe({
          next: (summary) => {
            this.stats.set([
              { label: 'Total users', value: summary.totalUsersCount, icon: 'group' },
              { label: 'Active users', value: summary.activeUsersCount, icon: 'person_check' },
              { label: 'Inactive users', value: summary.inactiveUsersCount, icon: 'person_off' },
              { label: 'Patients', value: summary.patientsCount, icon: 'personal_injury' },
              { label: 'Doctors', value: summary.doctorsCount, icon: 'medical_information' },
              { label: 'Hospitals', value: summary.hospitalsCount, icon: 'local_hospital' },
              { label: 'Service providers', value: summary.medicalServiceProvidersCount, icon: 'medical_services' },
              { label: 'Specialties', value: summary.medicalSpecialtiesCount, icon: 'category' },
              { label: 'Insurance companies', value: summary.insuranceCompaniesCount, icon: 'fact_check' },
              { label: 'Service categories', value: summary.medicalServiceCategoriesCount, icon: 'list_alt' },
              { label: 'Visible reviews', value: summary.visibleReviewsCount, icon: 'visibility' },
              { label: 'Hidden reviews', value: summary.hiddenReviewsCount, icon: 'visibility_off' },
            ]);
            this.recentRegistrations.set(summary.recentRegistrations);
            this.loading.set(false);
          },
          error: (error: unknown) => this.handleLoadError(error),
        });
        break;

      default:
        this.stats.set([]);
        this.loading.set(false);
    }
  }

  private handleLoadError(error: unknown): void {
    this.loading.set(false);
    this.stats.set([]);
    this.error.set(friendlyMessageOf(error, 'Could not load the dashboard.'));
  }

  /**
   * Only ever called from the Patient's own button click below - never automatically. Finds
   * the single nearest hospital for this session only; the coordinates are not stored.
   */
  protected findClosestHospital(): void {
    this.closestHospitalMessage.set(null);
    this.findingClosestHospital.set(true);

    this.geolocation
      .getCurrentPosition()
      .then((coords) => {
        this.hospitalDiscovery
          .searchNearby({
            latitude: coords.latitude,
            longitude: coords.longitude,
            radiusKm: 100,
            page: 1,
            pageSize: 1,
          })
          .subscribe({
            next: (result) => {
              this.findingClosestHospital.set(false);
              if (result.items.length === 0) {
                this.closestHospitalMessage.set('No hospital with a set location was found within 100 km.');
                return;
              }
              this.closestHospital.set(result.items[0]);
            },
            error: () => {
              this.findingClosestHospital.set(false);
              this.closestHospitalMessage.set('Could not search for nearby hospitals.');
            },
          });
      })
      .catch((error: unknown) => {
        this.findingClosestHospital.set(false);

        if (error instanceof GeolocationFailure) {
          const messages: Record<typeof error.reason, string> = {
            denied: 'Location permission was denied.',
            unavailable: 'Your location could not be determined.',
            timeout: 'Getting your location took too long.',
          };
          this.closestHospitalMessage.set(messages[error.reason]);
          return;
        }

        this.closestHospitalMessage.set('Your location could not be determined.');
      });
  }
}
