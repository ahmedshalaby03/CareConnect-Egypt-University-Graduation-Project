import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ROLE_LABELS, UserRole } from '../../../core/models/user.model';
import { HospitalDirectoryItem } from '../../../core/models/directory.model';
import { AuthService } from '../../../core/services/auth.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { BloodRequestService } from '../../../core/services/blood-request.service';
import { BloodStockService } from '../../../core/services/blood-stock.service';
import { GeolocationFailure, GeolocationService } from '../../../core/services/geolocation.service';
import { HospitalDiscoveryService } from '../../../core/services/hospital-discovery.service';
import { HospitalLocationService } from '../../../core/services/hospital-location.service';
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
    comingSoon: ['View medical records'],
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
    ],
    comingSoon: ['Departments & staff', 'Bed availability'],
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
      {
        label: 'Blood bank',
        description: 'Search hospitals for available blood groups.',
        route: '/blood-bank',
        icon: 'bloodtype',
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
  imports: [DatePipe, RouterLink, MatIconModule, MatButtonModule],
  templateUrl: './role-dashboard.html',
  styleUrl: './role-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleDashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly appointments = inject(AppointmentService);
  private readonly insuranceRequests = inject(InsuranceRequestService);
  private readonly bloodRequests = inject(BloodRequestService);
  private readonly bloodStock = inject(BloodStockService);
  private readonly hospitalLocation = inject(HospitalLocationService);
  private readonly hospitalDiscovery = inject(HospitalDiscoveryService);
  private readonly geolocation = inject(GeolocationService);

  /** Set from the route data, so one component serves all four role dashboards. */
  readonly role = input.required<UserRole>();

  protected readonly user = this.auth.currentUser;

  protected readonly config = computed(() => DASHBOARDS[this.role()]);
  protected readonly roleLabel = computed(() => ROLE_LABELS[this.role()]);

  protected readonly stats = signal<StatTile[] | null>(null);

  /**
   * Session-only: filled in solely when the Patient clicks the button below, never on load.
   * The coordinates behind it are used for one search request and are not kept anywhere.
   */
  protected readonly closestHospital = signal<HospitalDirectoryItem | null>(null);
  protected readonly findingClosestHospital = signal(false);
  protected readonly closestHospitalMessage = signal<string | null>(null);

  ngOnInit(): void {
    // Scoped to the signed-in profile server-side; this just decides which call to make.
    switch (this.role()) {
      case 'Patient':
        forkJoin({
          appointments: this.appointments.getPatientDashboardStats(),
          insurance: this.insuranceRequests.getPatientDashboardStats(),
          blood: this.bloodRequests.getPatientDashboardStats(),
        }).subscribe({
          next: ({ appointments: s, insurance: i, blood: b }) =>
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
              { label: 'Pending blood requests', value: b.pendingCount, icon: 'bloodtype' },
              { label: 'Approved blood requests', value: b.approvedCount, icon: 'water_drop' },
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
          blood: this.bloodRequests.getHospitalDashboardStats(),
          location: this.hospitalLocation.get(),
        }).subscribe({
          next: ({ appointments: s, insurance: i, blood: b, location: loc }) =>
            this.stats.set([
              { label: "Today's appointments", value: s.todayCount, icon: 'today' },
              { label: 'Pending appointments', value: s.pendingCount, icon: 'hourglass_top' },
              { label: 'Active approved doctors', value: s.activeApprovedDoctorsCount, icon: 'groups' },
              { label: 'New insurance requests', value: i.pendingCount, icon: 'fact_check' },
              { label: 'Insurance under review', value: i.underReviewCount, icon: 'pending_actions' },
              { label: 'Approved this month', value: i.approvedThisMonthCount, icon: 'task_alt' },
              { label: 'Rejected this month', value: i.rejectedThisMonthCount, icon: 'block' },
              { label: 'Total blood units available', value: b.totalAvailableUnits, icon: 'bloodtype' },
              { label: 'Blood groups below minimum', value: b.bloodGroupsBelowMinimumCount, icon: 'warning' },
              { label: 'Pending blood requests', value: b.pendingRequestsCount, icon: 'water_drop' },
              { label: 'Emergency blood requests', value: b.emergencyRequestsCount, icon: 'emergency' },
              { label: 'Approved, awaiting fulfillment', value: b.approvedAwaitingFulfillmentCount, icon: 'inventory' },
              {
                label: 'Location status',
                value: loc.isLocationCompleted ? 'Complete' : 'Incomplete',
                icon: loc.isLocationCompleted ? 'near_me' : 'location_off',
              },
              {
                label: 'Coordinates',
                value: loc.latitude !== null && loc.longitude !== null ? 'Set' : 'Missing',
                icon: 'my_location',
              },
            ]),
          error: () => this.stats.set([]),
        });
        break;

      case 'SuperAdmin':
        forkJoin({
          blood: this.bloodStock.getSuperAdminDashboardStats(),
          location: this.hospitalDiscovery.getSuperAdminDashboardStats(),
        }).subscribe({
          next: ({ blood: b, location: loc }) =>
            this.stats.set([
              { label: 'Hospitals with blood stock', value: b.hospitalsWithStockCount, icon: 'local_hospital' },
              { label: 'Active blood stock records', value: b.activeBloodStockRecordsCount, icon: 'bloodtype' },
              {
                label: 'Hospitals with completed locations',
                value: loc.activeHospitalsWithCompletedLocationCount,
                icon: 'near_me',
              },
              {
                label: 'Hospitals missing coordinates',
                value: loc.activeHospitalsMissingCoordinatesCount,
                icon: 'location_off',
              },
              { label: 'Governorates covered', value: loc.governoratesCovered.length, icon: 'map' },
            ]),
          error: () => this.stats.set([]),
        });
        break;

      default:
        this.stats.set([]);
    }
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
