/** Numeric values mirror CareConnect.Domain.Enums.NotificationType JSON output. */
export type NotificationType = 1 | 2 | 3 | 4;

/** Numeric values mirror CareConnect.Domain.Enums.NotificationCategory JSON output. */
export type NotificationCategory = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

export const NOTIFICATION_CATEGORIES: NotificationCategory[] = [
  1,
  2,
  3,
  4,
  5,
  6,
  7,
  8,
];

export const NOTIFICATION_CATEGORY_LABELS: Record<NotificationCategory, string> = {
  1: 'Appointments',
  2: 'Insurance',
  3: 'Blood bank',
  4: 'Medical services',
  5: 'Hospital affiliations',
  6: 'Reviews',
  7: 'Account',
  8: 'System',
};

export interface AppNotification {
  id: string;
  type: NotificationType;
  typeName: string;
  category: NotificationCategory;
  categoryName: string;
  title: string;
  message: string;
  relatedEntityType: string | null;
  relatedEntityTypeName: string | null;
  relatedEntityId: string | null;
  actionRoute: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface NotificationFilter {
  isRead?: boolean | null;
  category?: NotificationCategory | null;
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  page: number;
  pageSize: number;
  sortDirection?: 'asc' | 'desc';
}

export interface NotificationUnreadCount {
  unreadCount: number;
}
