import { Component, computed, input } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import type { TagCount } from '../api/types';

@Component({
  selector: 'app-tag-filter',
  imports: [RouterLink],
  template: `
    <div class="flex flex-wrap gap-2">
      @for (tag of tags.value(); track tag.name) {
        <a
          [routerLink]="[]"
          [queryParams]="{ tag: activeTag() === tag.name ? null : tag.name }"
          queryParamsHandling="merge"
          [attr.data-testid]="'tag-chip-' + tag.name"
          class="rounded-full border px-3 py-1 text-sm"
          [class]="
            activeTag() === tag.name
              ? 'border-[var(--color-accent)] bg-[var(--color-accent)] text-white'
              : 'border-[var(--color-border)] text-[var(--color-ink-muted)]'
          "
        >
          {{ tag.name }} ({{ tag.count }})
        </a>
      }
    </div>
  `,
})
export class TagFilter {
  // withComponentInputBinding() sets this to `undefined` (not the declared
  // default) when the `tag` query param is absent - normalize with `?? ''`.
  readonly active = input<string>('');
  protected readonly activeTag = computed(() => this.active() ?? '');
  protected readonly tags = httpResource<TagCount[]>(() => '/api/tags');
}
