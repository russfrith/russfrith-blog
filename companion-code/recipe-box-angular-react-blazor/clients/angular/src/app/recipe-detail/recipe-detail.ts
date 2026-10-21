import { Component, computed, inject, input, linkedSignal, numberAttribute, signal } from '@angular/core';
import { HttpClient, httpResource } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ServingsStepper } from './servings-stepper';
import { RecipeCount } from '../recipe-count';
import { formatQuantity, scaledQuantity } from '../quantities';
import type { Recipe } from '../api/types';

@Component({
  selector: 'app-recipe-detail',
  imports: [RouterLink, ServingsStepper],
  templateUrl: './recipe-detail.html',
})
export class RecipeDetail {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly recipeCount = inject(RecipeCount);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly recipe = httpResource<Recipe>(() => `/api/recipes/${this.id()}`);
  protected readonly servings = linkedSignal(() => this.recipe.value()?.servings ?? 1);
  protected readonly confirming = signal(false);

  protected readonly quantities = computed(() => {
    const recipe = this.recipe.value();
    if (!recipe) return [];
    return recipe.ingredients.map((ingredient) =>
      formatQuantity(scaledQuantity(ingredient.quantity, recipe.servings, this.servings())),
    );
  });

  protected async deleteRecipe(): Promise<void> {
    await firstValueFrom(this.http.delete(`/api/recipes/${this.id()}`));
    this.recipeCount.refresh();
    this.router.navigateByUrl('/');
  }
}
