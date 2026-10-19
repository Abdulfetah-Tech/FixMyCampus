import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';
import { LoginComponent } from './features/auth/login/login.component';
import { TicketListComponent } from './features/tickets/ticket-list/ticket-list.component';
import { TicketDetailComponent } from './features/tickets/ticket-detail/ticket-detail.component';
import { CreateTicketComponent } from './features/tickets/create-ticket/create-ticket.component';
import { MyTicketsComponent } from './features/tickets/my-tickets/my-tickets.component';
import { AdminDashboardComponent } from './features/admin/dashboard/admin-dashboard.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { UserApprovalsComponent } from './features/admin/users/user-approvals.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    TicketListComponent,
    TicketDetailComponent,
    CreateTicketComponent,
    MyTicketsComponent,
    AdminDashboardComponent,
    RegisterComponent,
    UserApprovalsComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    AppRoutingModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
