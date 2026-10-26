// The same literal string is simultaneously the error-dictionary key, the
// test id, and the server's own error key - no path translation needed.
export function FieldError({ fieldKey, errors }: { fieldKey: string; errors: Record<string, string[]> }) {
  const message = errors[fieldKey]?.[0];
  if (!message) return null;
  return (
    <p data-testid={`form-error-${fieldKey}`} className="mt-1 text-sm text-[var(--color-danger)]">
      {message}
    </p>
  );
}
