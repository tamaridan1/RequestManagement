import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {RequestDto, RequestQueryParams, PagedResult, RequestSummary, StatusHistory, UpdateStatusRequest, BulkUpdateRequest, BulkUpdateResult} from '../models/request.models';

@Injectable({ providedIn: 'root' })
export class RequestService {
  private http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5066/api/requests';

  getRequests(query: RequestQueryParams): Observable<PagedResult<RequestDto>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

    if (query.search) params = params.set('search', query.search);
    if (query.status != null) params = params.set('status', query.status);
    if (query.priority != null) params = params.set('priority', query.priority);
    if (query.organizationName) params = params.set('organizationName', query.organizationName);
    if (query.assignedTo) params = params.set('assignedTo', query.assignedTo);
    if (query.createdFrom) params = params.set('createdFrom', query.createdFrom);
    if (query.createdTo) params = params.set('createdTo', query.createdTo);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDescending != null) params = params.set('sortDescending', query.sortDescending);

    return this.http.get<PagedResult<RequestDto>>(this.baseUrl, { params });
  }

  getSummary(): Observable<RequestSummary> {
    return this.http.get<RequestSummary>(`${this.baseUrl}/summary`);
  }

  getHistory(id: number): Observable<StatusHistory[]> {
    return this.http.get<StatusHistory[]>(`${this.baseUrl}/${id}/history`);
  }

  updateStatus(id: number, body: UpdateStatusRequest): Observable<RequestDto> {
    return this.http.put<RequestDto>(`${this.baseUrl}/${id}/status`, body);
  }

  bulkUpdateStatus(body: BulkUpdateRequest): Observable<BulkUpdateResult> {
    return this.http.post<BulkUpdateResult>(`${this.baseUrl}/bulk-status`, body);
  }
}
