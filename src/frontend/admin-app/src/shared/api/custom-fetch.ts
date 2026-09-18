const IDEMPOTENCY_HEADER = "Idempotency-Key";

const methodsNeedingIdempotencyKey = new Set(["POST", "PUT", "PATCH", "DELETE"]);

const extractMethod = (input: RequestInfo | URL, init?: RequestInit): string => {
  if (input instanceof Request) {
    return init?.method ?? input.method;
  }
  return init?.method ?? "GET";
};

export const decorateWithIdempotencyKeys = (
  fetch: typeof globalThis.fetch,
): typeof globalThis.fetch => {
  return (input, init) => {
    const method = extractMethod(input, init);

    if (!methodsNeedingIdempotencyKey.has(method.toUpperCase())) {
      return fetch(input, init);
    }

    const headers = new Headers(init?.headers);
    headers.set(IDEMPOTENCY_HEADER, crypto.randomUUID());

    return fetch(input, { ...init, headers });
  };
};
