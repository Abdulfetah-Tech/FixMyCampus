import { Component, OnInit } from '@angular/core';
import { TicketService } from 'src/app/core/services/ticket.service';
import { Building, DashboardSummary, Ticket, Technician, TicketStatus } from 'src/app/shared/models/ticket.model';
import { getApiErrorMessage } from 'src/app/core/utils/api-error-message';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  tickets: Ticket[] = [];
  technicians: Technician[] = [];
  buildings: Building[] = [];
  summary: DashboardSummary | null = null;
  buildingFilter: number | null = null;
  statusFilter: TicketStatus | '' = '';
  loading = false;
  errorMessage = '';
  technicianErrorMessage = '';
  loadingTechnicians = false;
  busyTicketId: number | null = null;

  constructor(private ticketService: TicketService) {}

  ngOnInit(): void {
    this.loadTickets();
    this.loadTechnicians();
    this.loadBuildings();
    this.loadSummary();
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
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = getApiErrorMessage(err, 'Unable to load tickets.');
      }
    });
  }

  loadBuildings(): void {
    this.ticketService.getBuildings().subscribe({
      next: (buildings) => this.buildings = buildings,
      error: (err) => this.errorMessage = getApiErrorMessage(err, 'Unable to load campus buildings.')
    });
  }

  loadSummary(): void {
    this.ticketService.getDashboardSummary().subscribe({
      next: (summary) => this.summary = summary,
      error: (err) => this.errorMessage = getApiErrorMessage(err, 'Unable to load the dashboard summary.')
    });
  }

  loadTechnicians(): void {
    this.loadingTechnicians = true;
    this.technicianErrorMessage = '';
    this.ticketService.getTechnicians().subscribe({
      next: (techs) => {
        this.technicians = techs;
        this.loadingTechnicians = false;
      },
      error: (err) => {
        this.loadingTechnicians = false;
        this.technicianErrorMessage = getApiErrorMessage(err, 'Unable to load the technician list.');
      }
    });
  }

  assignTechnician(ticketId: number, technicianId: number): void {
    if (!technicianId || this.busyTicketId !== null) {
      return;
    }

    this.errorMessage = '';
    this.busyTicketId = ticketId;
    this.ticketService.assignTechnician(ticketId, { technicianId }).subscribe({
      next: () => {
        this.busyTicketId = null;
        this.loadTickets();
        this.loadSummary();
      },
      error: (err) => {
        this.busyTicketId = null;
        this.errorMessage = getApiErrorMessage(err, 'Unable to assign this ticket. Tickets can only be assigned while they are New.');
      }
    });
  }

  updateStatus(ticketId: number, status: TicketStatus): void {
    if (this.busyTicketId !== null) {
      return;
    }

    this.errorMessage = '';
    this.busyTicketId = ticketId;
    this.ticketService.updateStatus(ticketId, { newStatus: status }).subscribe({
      next: () => {
        this.busyTicketId = null;
        this.loadTickets();
        this.loadSummary();
      },
      error: (err) => {
        this.busyTicketId = null;
        this.errorMessage = getApiErrorMessage(err, 'Unable to update ticket status.');
      }
    });
  }

  get unresolvedCount(): number {
    return this.summary?.openTickets ?? this.tickets.filter((ticket) => ticket.status !== 'Resolved').length;
  }

  get newCount(): number {
    return this.summary?.newTickets ?? this.tickets.filter((ticket) => ticket.status === 'New').length;
  }

  get highUrgencyCount(): number {
    return this.summary?.overdueHighUrgency ?? this.tickets.filter((ticket) => ticket.urgency === 'High' && ticket.status !== 'Resolved').length;
  }
}
