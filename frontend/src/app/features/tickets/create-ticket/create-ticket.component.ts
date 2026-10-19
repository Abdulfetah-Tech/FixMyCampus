import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TicketService } from 'src/app/core/services/ticket.service';
import { Building, Ticket, TicketCategory } from 'src/app/shared/models/ticket.model';
import { getApiErrorMessage } from 'src/app/core/utils/api-error-message';

@Component({
  selector: 'app-create-ticket',
  templateUrl: './create-ticket.component.html',
  styleUrls: ['./create-ticket.component.css']
})
export class CreateTicketComponent implements OnInit {
  ticketForm: FormGroup;
  buildings: Building[] = [];
  readonly categories: TicketCategory[] = ['Computer', 'Network', 'Projector', 'Plumbing', 'Electrical', 'Other'];
  errorMessage = '';
  buildingErrorMessage = '';
  successMessage = '';
  createdTicket: Ticket | null = null;
  submitting = false;
  loadingBuildings = true;

  constructor(private fb: FormBuilder, private ticketService: TicketService) {
    this.ticketForm = this.fb.group({
      category: ['', Validators.required],
      buildingId: ['', Validators.required],
      room: ['', Validators.required],
      description: ['', [Validators.required, Validators.minLength(10)]],
      urgency: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.ticketService.getBuildings().subscribe({
      next: (buildings) => {
        this.buildings = buildings;
        this.loadingBuildings = false;
      },
      error: (err) => {
        this.buildingErrorMessage = getApiErrorMessage(err, 'Unable to load campus buildings.');
        this.loadingBuildings = false;
      }
    });
  }

  submit(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.submitting || this.ticketForm.invalid) {
      this.ticketForm.markAllAsTouched();
      if (this.ticketForm.invalid) {
        this.errorMessage = 'Please complete all required fields. Description must be at least 10 characters.';
      }
      return;
    }

    this.submitting = true;
    this.createdTicket = null;
    this.ticketService.createTicket(this.ticketForm.getRawValue()).subscribe({
      next: (ticket) => {
        this.submitting = false;
        this.createdTicket = ticket;
        this.successMessage = 'Ticket created successfully.';
        this.ticketForm.reset();
      },
      error: (err) => {
        this.submitting = false;
        this.errorMessage = getApiErrorMessage(err, 'Unable to create the ticket right now.');
      }
    });
  }
}
