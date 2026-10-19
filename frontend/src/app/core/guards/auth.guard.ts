import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.authService.restoreSession().pipe(
      map((user) => user !== null || this.router.createUrlTree(['/login'])),
      catchError(() => of(this.router.createUrlTree(['/login'])))
    );
  }
}
