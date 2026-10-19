import { Component, OnInit } from '@angular/core';
import { TicketService } from 'src/app/core/services/ticket.service';
import { Building, Ticket, TicketStatus } from 'src/app/shared/models/ticket.model';
import { getApiErrorMessage } from 'src/app/core/utils/api-error-message';

@Component({
  selector: 'app-ticket-list',
  templateUrl: './ticket-list.component.html',
  styleUrls: ['./ticket-list.component.css']
})
export class TicketListComponent implements OnInit {
  tickets: Ticket[] = [];
  buildings: Building[] = [];
  buildingFilter: number | null = null;
  statusFilter: TicketStatus | '' = '';
  totalCount = 0;
  loading = false;
  errorMessage = '';

  constructor(private ticketService: TicketService) {}

  ngOnInit(): void {
    this.ticketService.getBuildings().subscribe({
      next: (buildings) => this.buildings = buildings,
      error: (err) => this.errorMessage = getApiErrorMessage(err, 'Unable to load campus buildings.')
    });
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading = true;
    this.errorMessage = '';

    this.ticketService.getTickets({
      buildingId: this.buildingFilter ?? undefined,
      status: this.statusFilter || undefined,
      pageSize: 50
    }).subscribe({
      next: (result) => {
        this.tickets = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getApiErrorMessage(err, 'Unable to load the campus feed.');
      }
    });
  }
}
