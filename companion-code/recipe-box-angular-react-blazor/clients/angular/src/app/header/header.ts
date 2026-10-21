import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RecipeCount } from '../recipe-count';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header class="border-b border-[var(--color-border)] bg-[var(--color-paper)]">
      <div class="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
        <div>
          <a routerLink="/" class="font-serif text-xl text-[var(--color-ink)]">Recipe Box</a>
          <p data-testid="header-count" class="text-sm text-[var(--color-ink-muted)]">
            @if (count.count(); as n) {
              {{ n }} {{ n === 1 ? 'recipe' : 'recipes' }}
            }
          </p>
        </div>
        <a
          routerLink="/recipes/new"
          data-testid="new-recipe-button"
          class="rounded-[var(--radius-control)] bg-[var(--color-accent)] px-4 py-2 text-white hover:bg-[var(--color-accent-hover)]"
        >
          New recipe
        </a>
      </div>
    </header>
  `,
})
export class Header {
  protected readonly count = inject(RecipeCount);
}
