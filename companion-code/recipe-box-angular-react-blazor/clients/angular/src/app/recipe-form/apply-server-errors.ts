import type { WritableSignal } from '@angular/core';
import { FormGroup } from '@angular/forms';
import type { ValidationProblem } from '../api/types';

// Maps server error keys (spec.md's grammar: `ingredients[0].quantity`,
// `tags[1]`, `title`, bare `tags`, and `file` for image errors) onto form
// controls. `ingredients[0].quantity` -> `ingredients.0.quantity`, which
// FormGroup#get resolves through the FormArray by index - safe because
// array order is exactly what was sent to the server.
export function applyServerErrors(form: FormGroup, errors: ValidationProblem['errors'], fileError: WritableSignal<string | null>): void {
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const message = messages[0];
    if (key === 'file') {
      fileError.set(message);
      continue;
    }
    const path = key.replace(/\[(\d+)\]/g, '.$1');
    const control = path === '' ? form : form.get(path);
    control?.setErrors({ server: message });
  }
}
