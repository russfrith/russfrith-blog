import type { ValidationProblem } from './api/types';

// fetch() doesn't reject on 4xx/5xx the way Angular's HttpClient does, so
// this wrapper throws instead - every caller can just try/catch.
export class HttpError extends Error {
  status: number;
  errors?: ValidationProblem['errors'];

  constructor(status: number, errors?: ValidationProblem['errors']) {
    super(`HTTP ${status}`);
    this.status = status;
    this.errors = errors;
  }
}

export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (response.status === 204) return undefined as T;
  if (!response.ok) {
    const problem = await response.json().catch(() => undefined);
    throw new HttpError(response.status, problem?.errors);
  }
  return response.json();
}
