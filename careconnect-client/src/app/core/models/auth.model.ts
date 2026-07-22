import { User, UserRole } from './user.model';

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string | null;
  password: string;
  confirmPassword: string;
  role: UserRole;
}

export interface RegisterResponse {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: User;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

/** What the TokenService keeps in storage. Passwords are never part of this. */
export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: User;
}
