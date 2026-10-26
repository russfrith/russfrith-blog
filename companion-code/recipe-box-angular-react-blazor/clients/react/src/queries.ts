import { queryOptions } from '@tanstack/react-query';
import { request } from './http';
import type { Recipe, RecipeSummary, TagCount } from './api/types';

// queryOptions() factories, not a service/hook layer - the query cache
// itself is the shared state (see recipeCount.ts). One definition each
// serves useQuery(...), setQueryData(...), and the header's shared key.
export const recipesQuery = (search: string, tag: string) =>
  queryOptions({
    queryKey: ['recipes', { search, tag }],
    queryFn: () => request<RecipeSummary[]>(`/api/recipes?${new URLSearchParams({ search, tag })}`),
  });

export const recipeQuery = (id: number) =>
  queryOptions({
    queryKey: ['recipe', id],
    queryFn: () => request<Recipe>(`/api/recipes/${id}`),
  });

export const tagsQuery = () =>
  queryOptions({
    queryKey: ['tags'],
    queryFn: () => request<TagCount[]>('/api/tags'),
  });
