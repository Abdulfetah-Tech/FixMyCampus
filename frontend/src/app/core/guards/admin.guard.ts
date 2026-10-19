import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.authService.restoreSession().pipe(
      map((user) => {
        if (!user) return this.router.createUrlTree(['/login']);
        if (user.role === 'Admin') return true;
        return this.router.createUrlTree([user.role === 'Technician' ? '/technician' : '/reporter']);
      }),
      catchError(() => of(this.router.createUrlTree(['/login'])))
    );
  }
}
