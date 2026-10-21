import { Component, inject, input } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { FieldError } from './field-error';

@Component({
  selector: 'app-step-rows',
  imports: [ReactiveFormsModule, FieldError],
  templateUrl: './step-rows.html',
})
export class StepRows {
  private readonly fb = inject(FormBuilder);
  readonly steps = input.required<FormArray<FormControl<string>>>();
  readonly errorFor = input.required<(c: AbstractControl | null | undefined) => string | null>();

  protected addRow(): void {
    this.steps().push(this.fb.nonNullable.control(''));
  }

  protected removeRow(index: number): void {
    this.steps().removeAt(index);
  }

  protected moveUp(index: number): void {
    if (index === 0) return;
    const control = this.steps().at(index);
    this.steps().removeAt(index);
    this.steps().insert(index - 1, control);
  }

  protected moveDown(index: number): void {
    if (index === this.steps().length - 1) return;
    const control = this.steps().at(index);
    this.steps().removeAt(index);
    this.steps().insert(index + 1, control);
  }
}
