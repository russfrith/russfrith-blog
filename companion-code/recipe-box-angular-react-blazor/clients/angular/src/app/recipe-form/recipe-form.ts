import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { AbstractControl, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ingredientGroup, newTag, readError, recipeForm } from './recipe-rules';
import { applyServerErrors } from './apply-server-errors';
import { IngredientRows } from './ingredient-rows';
import { StepRows } from './step-rows';
import { TagInput } from './tag-input';
import { FieldError } from './field-error';
import { RecipeCount } from '../recipe-count';
import type { Recipe, RecipeInput } from '../api/types';

@Component({
  selector: 'app-recipe-form',
  imports: [ReactiveFormsModule, IngredientRows, StepRows, TagInput, FieldError],
  templateUrl: './recipe-form.html',
})
export class RecipeForm {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly recipeCount = inject(RecipeCount);

  readonly id = input<string>();
  private readonly idNumber = computed(() => (this.id() ? Number(this.id()) : undefined));

  protected readonly existing = httpResource<Recipe>(() => (this.idNumber() ? `/api/recipes/${this.idNumber()}` : undefined));
  protected readonly form = recipeForm(this.fb);
  protected readonly saving = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly createdId = signal<number | undefined>(undefined);
  protected readonly recipeId = computed(() => this.idNumber() ?? this.createdId());

  private readonly formTick = toSignal(this.form.events, { initialValue: null });
  protected readonly errorFor = (control: AbstractControl | null | undefined): string | null => {
    this.formTick();
    return readError(control);
  };

  constructor() {
    effect(() => {
      const recipe = this.existing.value();
      if (recipe) this.loadInto(recipe);
    });
  }

  private loadInto(recipe: Recipe): void {
    this.form.patchValue({
      title: recipe.title,
      description: recipe.description ?? '',
      servings: recipe.servings,
      prepMinutes: recipe.prepMinutes,
      cookMinutes: recipe.cookMinutes,
    });
    this.form.controls.tags.clear();
    for (const tag of recipe.tags) this.form.controls.tags.push(newTag(this.fb, tag));
    this.form.controls.ingredients.clear();
    for (const ing of recipe.ingredients) {
      const group = ingredientGroup(this.fb);
      group.patchValue({ quantity: ing.quantity, unit: ing.unit ?? '', name: ing.name });
      this.form.controls.ingredients.push(group);
    }
    this.form.controls.steps.clear();
    for (const step of recipe.steps) this.form.controls.steps.push(this.fb.nonNullable.control(step.instruction));
  }

  protected onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.selectedFile.set(file);
    this.fileError.set(null);
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.saving.set(true);
    try {
      const value = this.form.getRawValue();
      const body: RecipeInput = {
        title: value.title,
        description: value.description || null,
        servings: value.servings,
        prepMinutes: value.prepMinutes,
        cookMinutes: value.cookMinutes,
        tags: value.tags,
        ingredients: value.ingredients,
        steps: value.steps.map((instruction) => ({ instruction })),
      };

      const id = this.recipeId();
      const saved = id
        ? await firstValueFrom(this.http.put<Recipe>(`/api/recipes/${id}`, body))
        : await firstValueFrom(this.http.post<Recipe>('/api/recipes', body));
      this.createdId.set(saved.id);

      const file = this.selectedFile();
      if (file) {
        const formData = new FormData();
        formData.append('file', file);
        await firstValueFrom(this.http.post(`/api/recipes/${saved.id}/image`, formData));
      }

      this.recipeCount.refresh();
      this.router.navigateByUrl(`/recipes/${saved.id}`);
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 400) {
        applyServerErrors(this.form, err.error?.errors ?? {}, this.fileError);
      }
    } finally {
      this.saving.set(false);
    }
  }
}
