// Retries the Testing-only reset endpoint until the API is up, and fails
// fast with a clear message if the API isn't actually in Testing (the
// e2e compose override wasn't applied - see docker-compose.e2e.yml).
export default async function globalSetup(): Promise<void> {
  const baseURL = process.env['BASE_URL']!;
  const deadline = Date.now() + 30_000;

  for (;;) {
    try {
      const res = await fetch(`${baseURL}/api/test/reset`, { method: 'POST' });
      if (res.status === 204) return;
      if (res.status === 404) {
        throw new Error('API is not in Testing; use docker-compose.e2e.yml');
      }
    } catch (err) {
      if (err instanceof Error && err.message.startsWith('API is not in Testing')) throw err;
    }
    if (Date.now() > deadline) throw new Error(`API at ${baseURL} never became ready for reset`);
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
}
