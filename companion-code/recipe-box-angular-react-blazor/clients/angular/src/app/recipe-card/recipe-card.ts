import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { RecipeSummary } from '../api/types';

@Component({
  selector: 'app-recipe-card',
  imports: [RouterLink],
  template: `
    <a
      [routerLink]="['/recipes', recipe().id]"
      [attr.data-testid]="'recipe-card-' + recipe().id"
      class="block overflow-hidden rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white shadow-[var(--shadow-card)]"
    >
      @if (recipe().imageUrl; as src) {
        <img [src]="src" alt="" class="h-40 w-full object-cover" />
      } @else {
        <div class="flex h-40 w-full items-center justify-center bg-[var(--color-border)] text-[var(--color-ink-muted)]">
          No photo
        </div>
      }
      <div class="p-4">
        <h2 [attr.data-testid]="'recipe-card-title-' + recipe().id" class="font-serif text-lg">
          {{ recipe().title }}
        </h2>
        <p class="text-sm text-[var(--color-ink-muted)]">{{ recipe().tags.join(', ') }}</p>
        <p class="text-sm text-[var(--color-ink-muted)]">{{ totalMinutes() }} min</p>
      </div>
    </a>
  `,
})
export class RecipeCard {
  readonly recipe = input.required<RecipeSummary>();
  protected readonly totalMinutes = computed(() => this.recipe().prepMinutes + this.recipe().cookMinutes);
}
