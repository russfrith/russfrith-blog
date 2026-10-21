import { Routes } from '@angular/router';
import { RecipeList } from './recipe-list/recipe-list';
import { RecipeDetail } from './recipe-detail/recipe-detail';
import { RecipeForm } from './recipe-form/recipe-form';

export const routes: Routes = [
  { path: '', component: RecipeList },
  { path: 'recipes/new', component: RecipeForm },
  { path: 'recipes/:id/edit', component: RecipeForm },
  { path: 'recipes/:id', component: RecipeDetail },
];
