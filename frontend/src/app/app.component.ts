import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  constructor(private authService: AuthService, private router: Router) {}

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigate(['/login']),
      error: () => this.router.navigate(['/login'])
    });
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  getRole(): string | null {
    return this.authService.getUserRole();
  }

  getHomeRoute(): string {
    const role = this.getRole();
    return role === 'Admin' ? '/admin' : role === 'Technician' ? '/technician' : '/reporter';
  }
}
