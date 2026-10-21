import { Component, effect, inject, input } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RecipeCard } from '../recipe-card/recipe-card';
import { TagFilter } from './tag-filter';
import type { RecipeSummary } from '../api/types';

@Component({
  selector: 'app-recipe-list',
  imports: [ReactiveFormsModule, RouterLink, RecipeCard, TagFilter],
  templateUrl: './recipe-list.html',
})
export class RecipeList {
  private readonly router = inject(Router);
  readonly search = input('');
  readonly tag = input('');

  protected readonly searchControl = new FormControl('', { nonNullable: true });

  // withComponentInputBinding() sets these inputs to `undefined` (not the
  // declared default) when the query param is simply absent from the URL
  // - guard with `?? ''` or HttpParams serializes the literal string
  // "undefined" into the request.
  protected readonly recipes = httpResource<RecipeSummary[]>(() => ({
    url: '/api/recipes',
    params: { search: this.search() ?? '', tag: this.tag() ?? '' },
  }));

  constructor() {
    effect(() => this.searchControl.setValue(this.search() ?? '', { emitEvent: false }));
    this.searchControl.valueChanges.pipe(debounceTime(250), takeUntilDestroyed()).subscribe((value) => {
      this.navigate({ search: value || null });
    });
  }

  protected navigate(queryParams: Record<string, string | null>): void {
    this.router.navigate([], { queryParams, queryParamsHandling: 'merge', replaceUrl: true });
  }

  protected clearFilters(): void {
    this.searchControl.setValue('', { emitEvent: false });
    this.router.navigate([]);
  }
}
