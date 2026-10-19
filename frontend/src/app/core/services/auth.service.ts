import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, finalize, of, shareReplay, tap, throwError } from 'rxjs';
import { LoginRequest, RegisterRequest, UserResponse } from '../../shared/models/ticket.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;
  private readonly currentUserSubject = new BehaviorSubject<UserResponse | null>(null);
  private sessionChecked = false;
  private restoreRequest: Observable<UserResponse | null> | null = null;

  constructor(private http: HttpClient) {}

  login(payload: LoginRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.baseUrl}/login`, payload).pipe(
      tap((user) => this.setCurrentUser(user))
    );
  }

  register(payload: RegisterRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.baseUrl}/register`, payload);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {}).pipe(
      finalize(() => this.setCurrentUser(null))
    );
  }

  isAuthenticated(): boolean {
    return this.currentUserSubject.value !== null;
  }

  getUserRole(): string | null {
    return this.currentUserSubject.value?.role || null;
  }

  currentUser(): UserResponse | null {
    return this.currentUserSubject.value;
  }

  restoreSession(): Observable<UserResponse | null> {
    if (this.sessionChecked) {
      return of(this.currentUserSubject.value);
    }

    if (this.restoreRequest) {
      return this.restoreRequest;
    }

    this.restoreRequest = this.http.get<UserResponse>(`${this.baseUrl}/me`).pipe(
      catchError((error) => {
        if (error.status !== 401) {
          return throwError(() => error);
        }

        return this.http.post<UserResponse>(`${this.baseUrl}/refresh`, {}).pipe(
          catchError((refreshError) => refreshError.status === 401
            ? of(null)
            : throwError(() => refreshError))
        );
      }),
      tap((user) => {
        this.setCurrentUser(user);
        this.sessionChecked = true;
      }),
      finalize(() => {
        this.restoreRequest = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    return this.restoreRequest;
  }

  private setCurrentUser(user: UserResponse | null): void {
    this.currentUserSubject.next(user);
    this.sessionChecked = true;
  }
}
