import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Split screen: brand panel on the left, the login or registration form on the right. */
@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthLayout {
  protected readonly highlights = [
    'One account for patients, doctors, hospitals and service providers',
    'Role-based access enforced by the API, not just the browser',
    'Built for the Egyptian healthcare landscape',
  ];
}
