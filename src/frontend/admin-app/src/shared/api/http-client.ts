import { appConfig } from "../config";

export class ApiError extends Error {
  readonly status: number;
  readonly path: string;
  readonly body: unknown;

  constructor(path: string, status: number, body: unknown, options?: ErrorOptions) {
    super(`HTTP ${status} on ${path}`, options);
    this.name = "ApiError";
    this.status = status;
    this.path = path;
    this.body = body;
  }
}

export const httpClient = async <T>(path: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });

  if (!response.ok) {
    throw new ApiError(path, response.status, await readBody(response));
  }

  return (await response.json()) as T;
};

const readBody = async (response: Response): Promise<unknown> => {
  const text = await response.text();
  if (!text) {
    return undefined;
  }
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
};
