import { Component, inject, input } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { FieldError } from './field-error';
import { ingredientGroup } from './recipe-rules';

type IngredientGroup = ReturnType<typeof ingredientGroup>;

@Component({
  selector: 'app-ingredient-rows',
  imports: [ReactiveFormsModule, FieldError],
  templateUrl: './ingredient-rows.html',
})
export class IngredientRows {
  private readonly fb = inject(FormBuilder);
  readonly ingredients = input.required<FormArray<FormGroup<IngredientGroup['controls']>>>();
  readonly errorFor = input.required<(c: AbstractControl | null | undefined) => string | null>();

  protected addRow(): void {
    this.ingredients().push(ingredientGroup(this.fb));
  }

  protected removeRow(index: number): void {
    this.ingredients().removeAt(index);
  }
}
