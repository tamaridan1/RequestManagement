import { Component, OnInit, inject, signal, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestService } from '../../services/request.service';
import { StatusHistory, StatusLabels } from '../../models/request.models';

@Component({
  selector: 'app-history-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './history-dialog.html',
  styleUrl: './history-dialog.scss'
})
export class HistoryDialogComponent implements OnInit {
  private service = inject(RequestService);

  requestId = input.required<number>();
  close = output<void>();

  history = signal<StatusHistory[]>([]);
  loading = signal(true);
  StatusLabels = StatusLabels;

  ngOnInit(): void {
    this.service.getHistory(this.requestId()).subscribe({
      next: (h) => {
        this.history.set(h);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onClose(): void {
    this.close.emit();
  }
}
