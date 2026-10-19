import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApprovalStatus, UserAdminResponse } from '../../shared/models/ticket.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly usersUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getUsers(status?: ApprovalStatus): Observable<UserAdminResponse[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<UserAdminResponse[]>(this.usersUrl, { params });
  }

  approve(userId: string, role?: 'Reporter' | 'Technician'): Observable<UserAdminResponse> {
    return this.http.post<UserAdminResponse>(`${this.usersUrl}/${encodeURIComponent(userId)}/approve`, { role });
  }

  reject(userId: string, reason: string): Observable<UserAdminResponse> {
    return this.http.post<UserAdminResponse>(`${this.usersUrl}/${encodeURIComponent(userId)}/reject`, { reason });
  }
}
