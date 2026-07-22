/** Mirrors CareConnect.Application.Common.Models.ApiResponse<T>. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors?: string[] | null;
}

/** Mirrors CareConnect.Application.Common.Models.PagedResult<T>. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
