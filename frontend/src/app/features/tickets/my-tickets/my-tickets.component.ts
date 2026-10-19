import { Component, OnInit } from '@angular/core';
import { TicketService } from 'src/app/core/services/ticket.service';
import { Ticket } from 'src/app/shared/models/ticket.model';
import { getApiErrorMessage } from 'src/app/core/utils/api-error-message';

@Component({
  selector: 'app-my-tickets',
  templateUrl: './my-tickets.component.html',
  styleUrls: ['./my-tickets.component.css']
})
export class MyTicketsComponent implements OnInit {
  tickets: Ticket[] = [];
  errorMessage = '';
  loading = true;

  constructor(private ticketService: TicketService) {}

  ngOnInit(): void {
    this.ticketService.getMyTickets({ page: 1, pageSize: 50 }).subscribe({
      next: (result) => {
        this.tickets = result.items;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = getApiErrorMessage(err, 'Unable to load your reports.');
        this.loading = false;
      }
    });
  }
}
