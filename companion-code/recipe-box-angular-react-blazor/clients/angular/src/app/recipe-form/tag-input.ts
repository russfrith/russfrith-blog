import { Component, inject, input } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl } from '@angular/forms';
import { FieldError } from './field-error';
import { newTag } from './recipe-rules';

@Component({
  selector: 'app-tag-input',
  imports: [FieldError],
  template: `
    <div class="flex flex-wrap items-center gap-2">
      @for (tag of tags().controls; track $index) {
        <button
          type="button"
          [attr.data-testid]="'form-tag-' + tag.value"
          (click)="tags().removeAt($index)"
          class="rounded-full border border-[var(--color-border)] px-3 py-1 text-sm"
        >
          {{ tag.value }} ✕
        </button>
      }
      <input
        #input
        data-testid="form-tag-input"
        (keydown.enter)="add(input); $event.preventDefault()"
        placeholder="Add a tag and press Enter"
        class="rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
      />
    </div>
    <app-field-error fieldKey="tags" [message]="errorFor()(tags())" />
  `,
})
export class TagInput {
  private readonly fb = inject(FormBuilder);
  readonly tags = input.required<FormArray<FormControl<string>>>();
  readonly errorFor = input.required<(c: AbstractControl | null | undefined) => string | null>();

  protected add(input: HTMLInputElement): void {
    const value = input.value.trim().toLowerCase();
    input.value = '';
    if (!value) return;
    this.tags().push(newTag(this.fb, value));
  }
}
