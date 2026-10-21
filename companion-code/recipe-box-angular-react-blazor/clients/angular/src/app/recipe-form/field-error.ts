import { Component, input } from '@angular/core';

// Presentational only - the parent form computes `message` (it already
// has to be the one subscribed to `form.events` for zoneless reactivity,
// so there's no benefit to this component re-deriving it independently).
@Component({
  selector: 'app-field-error',
  template: `
    @if (message()) {
      <p [attr.data-testid]="'form-error-' + fieldKey()" class="mt-1 text-sm text-[var(--color-danger)]">{{ message() }}</p>
    }
  `,
})
export class FieldError {
  readonly fieldKey = input.required<string>();
  readonly message = input<string | null>(null);
}
