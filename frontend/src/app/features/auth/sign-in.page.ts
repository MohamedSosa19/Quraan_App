import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from './auth.service';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './sign-in.page.html',
  styleUrls: ['./auth.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(1)]],
  });
  readonly submitting = signal(false);
  readonly hasError = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.hasError.set(false);
    try {
      const { email, password } = this.form.getRawValue();
      await firstValueFrom(this.auth.signIn(email, password));
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
      await this.router.navigateByUrl(returnUrl);
    } catch {
      // Generic error — never disclose lockout / unknown-email distinction.
      this.hasError.set(true);
    } finally {
      this.submitting.set(false);
    }
  }
}
