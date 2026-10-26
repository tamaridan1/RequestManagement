import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, switchMap, debounceTime, distinctUntilChanged, startWith, combineLatest, BehaviorSubject } from 'rxjs';
import { RequestService } from '../../services/request.service';
import {RequestDto, RequestQueryParams, PagedResult, RequestStatus, RequestPriority, StatusLabels, PriorityLabels, BulkUpdateRequest} from '../../models/request.models';
import { SummaryComponent } from '../summary/summary';
import { HistoryDialogComponent } from '../history-dialog/history-dialog';

@Component({
  selector: 'app-requests-list',
  standalone: true,
  imports: [CommonModule, FormsModule, SummaryComponent, HistoryDialogComponent],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.scss'
})
export class RequestsListComponent implements OnInit {
  private service = inject(RequestService);
  private summaryComponent = viewChild(SummaryComponent);

  StatusLabels = StatusLabels;
  PriorityLabels = PriorityLabels;
  RequestStatus = RequestStatus;
  RequestPriority = RequestPriority;
  statusValues = Object.values(RequestStatus).filter(v => typeof v === 'number') as RequestStatus[];
  priorityValues = Object.values(RequestPriority).filter(v => typeof v === 'number') as RequestPriority[];

  result = signal<PagedResult<RequestDto> | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  historyRequestId = signal<number | null>(null);

  selectedIds = signal<Set<number>>(new Set());
  bulkStatus: RequestStatus | null = null;

  query: RequestQueryParams = {
    pageNumber: 1,
    pageSize: 10,
    sortBy: 'CreatedAt',
    sortDescending: true
  };

  private searchSubject = new Subject<string>();
  private reloadSubject = new BehaviorSubject<void>(undefined);

  ngOnInit(): void {
    const search$ = this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      startWith('')
    );

    combineLatest([search$, this.reloadSubject]).pipe(
      switchMap(([search]) => {
        this.loading.set(true);
        this.error.set(null);
        this.query.search = search || undefined;
        return this.service.getRequests(this.query);
      })
    ).subscribe({
      next: (res) => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('אירעה שגיאה בטעינת הנתונים.');
        this.loading.set(false);
      }
    });
  }

  onSearchChange(value: string): void {
    this.query.pageNumber = 1;
    this.searchSubject.next(value);
  }

  reload(): void {
    this.reloadSubject.next();
  }

  onFilterChange(): void {
    this.query.pageNumber = 1;
    this.reload();
  }

  onSort(field: string): void {
    if (this.query.sortBy === field) {
      this.query.sortDescending = !this.query.sortDescending;
    } else {
      this.query.sortBy = field;
      this.query.sortDescending = false;
    }
    this.reload();
  }

  goToPage(page: number): void {
    this.query.pageNumber = page;
    this.reload();
  }

  changeStatus(req: RequestDto, newStatus: RequestStatus): void {
    this.service.updateStatus(req.id, {
      newStatus,
      rowVersion: req.rowVersion,
      changedBy: 'משתמש'
    }).subscribe({
      next: () => {
        this.reload();
        this.summaryComponent()?.refresh();
      },
      error: (err) => {
        if (err.status === 409) {
          alert('הפנייה עודכנה על ידי משתמש אחר. מרענן את הנתונים.');
          this.reload();
        } else if (err.status === 400) {
          alert('מעבר סטטוס לא חוקי.');
        } else {
          alert('שגיאה בעדכון הסטטוס.');
        }
      }
    });
  }

  toggleSelect(id: number): void {
    const set = new Set(this.selectedIds());
    if (set.has(id)) set.delete(id); else set.add(id);
    this.selectedIds.set(set);
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelectAll(): void {
    const items = this.result()?.items ?? [];
    const allSelected = items.every(i => this.selectedIds().has(i.id));
    const set = new Set(this.selectedIds());
    if (allSelected) {
      items.forEach(i => set.delete(i.id));
    } else {
      items.forEach(i => set.add(i.id));
    }
    this.selectedIds.set(set);
  }

  allSelected(): boolean {
    const items = this.result()?.items ?? [];
    return items.length > 0 && items.every(i => this.isSelected(i.id));
  }

  applyBulk(): void {
    if (this.bulkStatus == null || this.selectedIds().size === 0) {
      alert('יש לבחור פניות וסטטוס.');
      return;
    }

    const items = (this.result()?.items ?? [])
      .filter(r => this.selectedIds().has(r.id))
      .map(r => ({ id: r.id, rowVersion: r.rowVersion }));

    const body: BulkUpdateRequest = {
      newStatus: this.bulkStatus,
      changedBy: 'משתמש',
      items
    };

    this.service.bulkUpdateStatus(body).subscribe({
      next: (res) => {
        alert(`עודכנו ${res.succeededCount} פניות, נכשלו ${res.failedCount}.`);
        this.selectedIds.set(new Set());
        this.bulkStatus = null;
        this.reload();
        this.summaryComponent()?.refresh();
      },
      error: () => alert('שגיאה בעדכון הקבוצתי.')
    });
  }

  openHistory(id: number): void {
    this.historyRequestId.set(id);
  }

  closeHistory(): void {
    this.historyRequestId.set(null);
  }
}
