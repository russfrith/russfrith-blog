import { Component, model } from '@angular/core';

@Component({
  selector: 'app-servings-stepper',
  template: `
    <div class="flex items-center gap-3">
      <button data-testid="servings-minus" (click)="step(-1)" [disabled]="servings() <= 1" class="h-8 w-8 rounded-full border">
        −
      </button>
      <span data-testid="servings-value" class="w-8 text-center">{{ servings() }}</span>
      <button data-testid="servings-plus" (click)="step(1)" [disabled]="servings() >= 48" class="h-8 w-8 rounded-full border">
        +
      </button>
    </div>
  `,
})
export class ServingsStepper {
  readonly servings = model.required<number>();

  protected step(delta: number): void {
    this.servings.update((value) => Math.min(48, Math.max(1, value + delta)));
  }
}
