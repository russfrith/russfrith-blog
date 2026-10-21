import { AbstractControl, FormArray, FormBuilder, FormControl, ValidationErrors, ValidatorFn } from '@angular/forms';

// Mirrors shared/RecipeBox.Shared/RecipeValidator.cs exactly - same rules,
// same message strings - so client and server errors read identically.
// Errors are keyed `rule` so FieldError can show `control.errors()['rule']`.
const rule = (message: string): ValidationErrors => ({ rule: message });

function length(min: number, max: number, message: string): ValidatorFn {
  return (c) => {
    const len = (c.value ?? '').trim().length;
    return len < min || len > max ? rule(message) : null;
  };
}

function range(min: number, max: number, message: string): ValidatorFn {
  return (c) => (c.value < min || c.value > max ? rule(message) : null);
}

function count(min: number, max: number, message: string): ValidatorFn {
  return (c) => {
    const len = (c as FormArray).length;
    return len < min || len > max ? rule(message) : null;
  };
}

function uniqueTag(): ValidatorFn {
  return (c: AbstractControl) => {
    const siblings = (c.parent as FormArray<FormControl<string>> | null)?.controls ?? [];
    const value = (c.value ?? '').trim().toLowerCase();
    const duplicate = siblings.filter((s) => s !== c && s.value.trim().toLowerCase() === value).length > 0;
    return duplicate ? rule('Tags must be unique.') : null;
  };
}

export function ingredientGroup(fb: FormBuilder) {
  return fb.nonNullable.group({
    quantity: [0, [range(0.0001, 10000, 'Quantity must be greater than 0 and at most 10000.')]],
    unit: [''],
    name: ['', [length(1, 100, 'Name must be 1 to 100 characters.')]],
  });
}

export function recipeForm(fb: FormBuilder) {
  return fb.nonNullable.group({
    title: ['', [length(3, 100, 'Title must be 3 to 100 characters.')]],
    description: ['', [length(0, 500, 'Description must be at most 500 characters.')]],
    servings: [4, [range(1, 48, 'Servings must be between 1 and 48.')]],
    prepMinutes: [0, [range(0, 1440, 'PrepMinutes must be between 0 and 1440.')]],
    cookMinutes: [0, [range(0, 1440, 'CookMinutes must be between 0 and 1440.')]],
    tags: fb.nonNullable.array<FormControl<string>>([], [count(0, 8, 'At most 8 tags are allowed.')]),
    ingredients: fb.array([ingredientGroup(fb)], [count(1, 50, 'At least 1 and at most 50 ingredients are required.')]),
    steps: fb.nonNullable.array<FormControl<string>>(
      [fb.nonNullable.control('', length(1, 1000, 'Instruction must be 1 to 1000 characters.'))],
      [count(1, 50, 'At least 1 and at most 50 steps are required.')],
    ),
  });
}

export function newTag(fb: FormBuilder, value = '') {
  return fb.nonNullable.control(value, [length(1, 30, 'Each tag must be 1 to 30 characters.'), uniqueTag()]);
}

// Reads the message to show for one control. Callers must first read some
// signal derived from `form.events` (see RecipeForm.formTick) so this
// re-runs under zoneless change detection - plain AbstractControl state
// (touched/dirty/errors) isn't itself a signal.
export function readError(control: AbstractControl | null | undefined): string | null {
  if (!control || control.valid || !(control.touched || control.dirty)) return null;
  return (control.errors?.['server'] as string) ?? (control.errors?.['rule'] as string) ?? null;
}
