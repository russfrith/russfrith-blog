import { Injectable, computed } from '@angular/core';
import { httpResource } from '@angular/common/http';
import type { RecipeSummary } from './api/types';

// The header's live recipe count - spec.md's one explicit cross-framework
// shared-state comparison point. Angular's idiom: a signal-based service.
@Injectable({ providedIn: 'root' })
export class RecipeCount {
  private readonly recipes = httpResource<RecipeSummary[]>(() => '/api/recipes');

  readonly count = computed(() => this.recipes.value()?.length);

  refresh(): void {
    this.recipes.reload();
  }
}
