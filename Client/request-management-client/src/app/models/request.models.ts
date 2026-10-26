export enum RequestStatus {
  New = 1,
  InProgress = 2,
  Waiting = 3,
  Completed = 4
}

export enum RequestPriority {
  Low = 1,
  Medium = 2,
  High = 3
}

export const StatusLabels: Record<RequestStatus, string> = {
  [RequestStatus.New]: 'חדש',
  [RequestStatus.InProgress]: 'בטיפול',
  [RequestStatus.Waiting]: 'ממתין',
  [RequestStatus.Completed]: 'הושלם'
};

export const PriorityLabels: Record<RequestPriority, string> = {
  [RequestPriority.Low]: 'נמוכה',
  [RequestPriority.Medium]: 'בינונית',
  [RequestPriority.High]: 'גבוהה'
};

export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface RequestQueryParams {
  pageNumber: number;
  pageSize: number;
  search?: string;
  status?: RequestStatus;
  priority?: RequestPriority;
  organizationName?: string;
  assignedTo?: string;
  createdFrom?: string;
  createdTo?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface RequestSummary {
  countByStatus: Record<number, number>;
  countByPriority: Record<number, number>;
}

export interface StatusHistory {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string | null;
}

export interface UpdateStatusRequest {
  newStatus: RequestStatus;
  rowVersion: string;
  changedBy?: string;
}

export interface BulkUpdateRequest {
  newStatus: RequestStatus;
  changedBy?: string;
  items: { id: number; rowVersion: string }[];
}

export interface BulkItemResult {
  id: number;
  success: boolean;
  error: string | null;
}

export interface BulkUpdateResult {
  totalRequested: number;
  succeededCount: number;
  failedCount: number;
  results: BulkItemResult[];
}
