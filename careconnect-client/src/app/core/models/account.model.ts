import { UserRole } from './user.model';

export interface AccountProfile {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
  profileImageUrl: string | null;
  hasProfileImage: boolean;
  roleProfileRoute: string | null;
}

export interface UpdateAccountProfileRequest {
  fullName: string;
  phoneNumber: string | null;
}
