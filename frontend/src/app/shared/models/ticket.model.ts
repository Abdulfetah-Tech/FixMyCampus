export type TicketStatus = 'New' | 'Assigned' | 'InProgress' | 'Resolved';
export type TicketUrgency = 'Low' | 'Medium' | 'High';
export type TicketCategory = 'Computer' | 'Network' | 'Projector' | 'Plumbing' | 'Electrical' | 'Other';
export type UserRole = 'Admin' | 'Reporter' | 'Technician';
export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected';

export interface Ticket {
  id: number;
  referenceCode: string;
  category: TicketCategory;
  building: string;
  buildingId: number;
  room: string;
  description: string;
  status: TicketStatus;
  urgency: TicketUrgency;
  reporterName: string;
  technicianName?: string;
  technicianId?: number;
  createdAt: string;
  updatedAt?: string;
  assignedAt?: string;
  resolvedAt?: string;
  confirmedFixedAt?: string;
  nextAllowedStatus?: TicketStatus;
  attachmentCount: number;
  history: TicketHistoryItem[];
  attachments: TicketAttachment[];
}

export interface TicketHistoryItem {
  fromStatus?: TicketStatus;
  toStatus: TicketStatus;
  changedByName: string;
  changedAt: string;
  note?: string;
}

export interface TicketAttachment {
  id: number;
  type: string;
  originalFileName: string;
  contentType: string;
  sizeInBytes: number;
  uploadedByName: string;
  createdAt: string;
  url: string;
}

export interface Technician {
  id: number;
  fullName: string;
  activeTicketCount: number;
}

export interface Building {
  id: number;
  name: string;
  code: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  universityId: string;
  requestedRole: 'Reporter' | 'Technician';
}

export interface UserResponse {
  id: string;
  fullName: string;
  email: string;
  role: UserRole | '';
  approvalStatus: ApprovalStatus;
}

export interface UserAdminResponse {
  id: string;
  fullName: string;
  email: string;
  universityId?: string;
  requestedRole: 'Reporter' | 'Technician';
  assignedRole?: UserRole;
  approvalStatus: ApprovalStatus;
  createdAt: string;
  reviewedAt?: string;
  rejectionReason?: string;
}

export interface CreateTicketRequest {
  category: TicketCategory;
  buildingId: number;
  room: string;
  description: string;
  urgency: TicketUrgency;
}

export interface AssignTicketRequest {
  technicianId: number;
}

export interface UpdateStatusRequest {
  newStatus: TicketStatus;
  note?: string;
}

export interface TicketQuery {
  buildingId?: number;
  status?: TicketStatus;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface DashboardSummary {
  openTickets: number;
  newTickets: number;
  assignedTickets: number;
  inProgressTickets: number;
  resolvedTickets: number;
  overdueHighUrgency: number;
  averageResolutionHours?: number;
  openByBuilding: { label: string; count: number }[];
  openByCategory: { label: string; count: number }[];
}
