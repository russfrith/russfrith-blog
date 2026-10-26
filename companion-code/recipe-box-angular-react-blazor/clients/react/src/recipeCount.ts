import { useQuery } from '@tanstack/react-query';
import { recipesQuery } from './queries';

// The header's live recipe count - spec.md's one explicit cross-framework
// shared-state comparison point. React's idiom: read the TanStack Query
// cache directly, no Context wrapper - the cache already is a shared,
// subscribable store, so wrapping it again would be the redundant
// indirection CLAUDE.md's "no abstractions" rule warns against. This uses
// the same query key as the unfiltered list, so on `/` they dedupe into
// one request; `queryClient.invalidateQueries({queryKey:['recipes']})`
// after create/delete is all "refresh" needs.
export function useRecipeCount() {
  return useQuery({ ...recipesQuery('', ''), select: (r) => r.length }).data;
}
