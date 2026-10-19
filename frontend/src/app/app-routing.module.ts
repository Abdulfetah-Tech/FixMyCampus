import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { AdminGuard } from './core/guards/admin.guard';
import { ReporterGuard } from './core/guards/reporter.guard';
import { TechnicianGuard } from './core/guards/technician.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { TicketListComponent } from './features/tickets/ticket-list/ticket-list.component';
import { CreateTicketComponent } from './features/tickets/create-ticket/create-ticket.component';
import { TicketDetailComponent } from './features/tickets/ticket-detail/ticket-detail.component';
import { MyTicketsComponent } from './features/tickets/my-tickets/my-tickets.component';
import { AdminDashboardComponent } from './features/admin/dashboard/admin-dashboard.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { UserApprovalsComponent } from './features/admin/users/user-approvals.component';

const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: 'reporter',
    canActivate: [ReporterGuard],
    children: [
      { path: '', component: MyTicketsComponent },
      { path: 'create', component: CreateTicketComponent },
      { path: 'my-tickets', component: MyTicketsComponent },
      { path: 'ticket/:id', component: TicketDetailComponent }
    ]
  },
  {
    path: 'admin',
    canActivate: [AdminGuard],
    children: [
      { path: '', component: AdminDashboardComponent },
      { path: 'users', component: UserApprovalsComponent }
    ]
  },
  { path: 'technician', component: TicketListComponent, canActivate: [TechnicianGuard] },
  { path: 'ticket/:id', component: TicketDetailComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: '/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
