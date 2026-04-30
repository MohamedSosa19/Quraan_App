import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from './auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './register.page.html',
  styleUrls: ['./auth.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(12)]],
    displayName: [''],
  });
  readonly submitting = signal(false);
  readonly hasError = signal(false);
  readonly emailTaken = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.hasError.set(false);
    this.emailTaken.set(false);
    try {
      const { email, password, displayName } = this.form.getRawValue();
      await firstValueFrom(this.auth.register(email, password, displayName || undefined));
      await this.router.navigateByUrl('/');
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 409) this.emailTaken.set(true);
      else this.hasError.set(true);
    } finally {
      this.submitting.set(false);
    }
  }
}
