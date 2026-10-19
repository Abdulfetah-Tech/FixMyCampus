import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import {
  AssignTicketRequest,
  Building,
  CreateTicketRequest,
  DashboardSummary,
  PagedResult,
  Technician,
  Ticket,
  TicketCategory,
  TicketQuery,
  TicketStatus,
  UpdateStatusRequest
} from '../../shared/models/ticket.model';
import { environment } from '../../../environments/environment';

interface TicketListItemDto {
  id: number;
  referenceCode: string;
  category: TicketCategory;
  buildingName: string;
  room: string;
  urgency: Ticket['urgency'];
  status: TicketStatus;
  reporterName: string;
  assignedTechnicianName?: string;
  createdAt: string;
  attachmentCount: number;
}

interface TicketDetailDto extends TicketListItemDto {
  buildingId: number;
  description: string;
  assignedTechnicianId?: number;
  updatedAt?: string;
  assignedAt?: string;
  resolvedAt?: string;
  confirmedFixedAt?: string;
  nextAllowedStatus?: TicketStatus;
  history: Ticket['history'];
  attachments: Ticket['attachments'];
}

interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private readonly apiUrl = environment.apiUrl;
  private readonly ticketsUrl = `${this.apiUrl}/tickets`;

  constructor(private http: HttpClient) {}

  getTickets(query: TicketQuery = {}): Observable<PagedResult<Ticket>> {
    return this.http.get<PagedResultDto<TicketListItemDto>>(this.ticketsUrl, {
      params: this.toParams(query)
    }).pipe(map((result) => this.mapPage(result, (item) => this.mapTicket(item))));
  }

  getMyTickets(query: TicketQuery = {}): Observable<PagedResult<Ticket>> {
    return this.http.get<PagedResultDto<TicketListItemDto>>(`${this.ticketsUrl}/mine`, {
      params: this.toParams(query)
    }).pipe(map((result) => this.mapPage(result, (item) => this.mapTicket(item))));
  }

  getTicket(id: number): Observable<Ticket> {
    return this.http.get<TicketDetailDto>(`${this.ticketsUrl}/${id}`)
      .pipe(map((item) => this.mapTicket(item)));
  }

  createTicket(payload: CreateTicketRequest): Observable<Ticket> {
    return this.http.post<TicketDetailDto>(this.ticketsUrl, payload)
      .pipe(map((item) => this.mapTicket(item)));
  }

  assignTechnician(id: number, payload: AssignTicketRequest): Observable<Ticket> {
    return this.http.post<TicketDetailDto>(`${this.ticketsUrl}/${id}/assign`, payload)
      .pipe(map((item) => this.mapTicket(item)));
  }

  updateStatus(id: number, payload: UpdateStatusRequest): Observable<Ticket> {
    return this.http.post<TicketDetailDto>(`${this.ticketsUrl}/${id}/status`, payload)
      .pipe(map((item) => this.mapTicket(item)));
  }

  confirmFix(id: number): Observable<Ticket> {
    return this.http.post<TicketDetailDto>(`${this.ticketsUrl}/${id}/confirm-fix`, {})
      .pipe(map((item) => this.mapTicket(item)));
  }

  getBuildings(): Observable<Building[]> {
    return this.http.get<Building[]>(`${this.apiUrl}/lookups/buildings`);
  }

  getTechnicians(): Observable<Technician[]> {
    return this.http.get<Technician[]>(`${this.apiUrl}/lookups/technicians`);
  }

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard/summary`);
  }

  private toParams(query: TicketQuery): HttpParams {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 50);

    if (query.buildingId) params = params.set('buildingId', query.buildingId);
    if (query.status) params = params.set('status', query.status);
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    return params;
  }

  private mapPage<TDto>(page: PagedResultDto<TDto>, mapItem: (item: TDto) => Ticket): PagedResult<Ticket> {
    return {
      items: page.items.map(mapItem),
      page: page.page,
      pageSize: page.pageSize,
      totalCount: page.totalCount,
      totalPages: page.totalPages
    };
  }

  private mapTicket(dto: TicketListItemDto | TicketDetailDto): Ticket {
    const detail = dto as Partial<TicketDetailDto>;

    return {
      id: dto.id,
      referenceCode: dto.referenceCode,
      category: dto.category,
      building: dto.buildingName,
      buildingId: detail.buildingId ?? 0,
      room: dto.room,
      description: detail.description ?? '',
      status: dto.status,
      urgency: dto.urgency,
      reporterName: dto.reporterName,
      technicianName: dto.assignedTechnicianName,
      technicianId: detail.assignedTechnicianId,
      createdAt: dto.createdAt,
      updatedAt: detail.updatedAt,
      assignedAt: detail.assignedAt,
      resolvedAt: detail.resolvedAt,
      confirmedFixedAt: detail.confirmedFixedAt,
      nextAllowedStatus: detail.nextAllowedStatus,
      attachmentCount: dto.attachmentCount,
      history: detail.history ?? [],
      attachments: detail.attachments ?? []
    };
  }
}
