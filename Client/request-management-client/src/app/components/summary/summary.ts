import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestService } from '../../services/request.service';
import { RequestSummary } from '../../models/request.models';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './summary.html',
  styleUrl: './summary.scss'
})
export class SummaryComponent implements OnInit {
  private service = inject(RequestService);

  summary = signal<RequestSummary | null>(null);

  statusLabels: Record<string, string> = {
    'New': 'חדש',
    'InProgress': 'בטיפול',
    'Waiting': 'ממתין',
    'Completed': 'הושלם'
  };

  priorityLabels: Record<string, string> = {
    'Low': 'נמוכה',
    'Medium': 'בינונית',
    'High': 'גבוהה'
  };

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.service.getSummary().subscribe({
      next: (s) => this.summary.set(s)
    });
  }

  toEntries(obj: Record<string, number>, labels: Record<string, string>): { label: string; count: number }[] {
    if (!obj) return [];
    return Object.keys(obj).map(key => ({
      label: labels[key] || key,
      count: obj[key]
    }));
  }
}
