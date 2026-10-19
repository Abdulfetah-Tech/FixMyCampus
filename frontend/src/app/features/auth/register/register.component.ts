import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { getApiErrorMessage } from '../../../core/utils/api-error-message';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  form: FormGroup;
  errorMessage = '';
  successMessage = '';
  submitting = false;

  constructor(private fb: FormBuilder, private authService: AuthService, private router: Router) {
    this.form = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
      universityId: ['', [Validators.required, Validators.maxLength(30)]],
      requestedRole: ['Reporter', Validators.required],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]]
    });
  }

  submit(): void {
    this.errorMessage = '';
    this.successMessage = '';
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.successMessage = 'Your account request was submitted. An administrator must approve it before you can sign in.';
        this.form.reset({ requestedRole: 'Reporter' });
        this.submitting = false;
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Unable to submit your account request.');
        this.submitting = false;
      }
    });
  }
}
