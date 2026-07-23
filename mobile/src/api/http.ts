import type { ProblemDetails } from './types';

export class ApiError extends Error {
  status: number;
  problem?: ProblemDetails;

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  accessToken?: string | null;
  signal?: AbortSignal;
}

/** Join a base URL and a path without doubling or dropping the slash. */
export function joinUrl(baseUrl: string, path: string): string {
  return `${baseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
}

/**
 * Thin fetch wrapper: JSON in/out, Bearer auth, and typed errors that surface the
 * API's RFC 7807 ProblemDetails `detail`/`title` as the message.
 */
export async function request<T>(
  baseUrl: string,
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { method = 'GET', body, accessToken, signal } = options;

  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`;

  let response: Response;
  try {
    response = await fetch(joinUrl(baseUrl, path), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
  } catch (err) {
    throw new ApiError(
      `Cannot reach the server at ${baseUrl}. Check the API URL and that the API is running.`,
      0,
    );
  }

  const text = await response.text();
  const data = text ? safeJson(text) : undefined;

  if (!response.ok) {
    const problem = data as ProblemDetails | undefined;
    const message =
      problem?.detail ||
      problem?.title ||
      defaultMessageForStatus(response.status);
    throw new ApiError(message, response.status, problem);
  }

  return data as T;
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function defaultMessageForStatus(status: number): string {
  switch (status) {
    case 400:
      return 'The request was invalid.';
    case 401:
      return 'Invalid username or password.';
    case 403:
      return 'This account is locked or not permitted.';
    case 404:
      return 'Not found.';
    case 429:
      return 'Too many attempts. Please wait and try again.';
    default:
      return `Request failed (HTTP ${status}).`;
  }
}
