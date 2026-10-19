import { HttpErrorResponse } from '@angular/common/http';

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error;
  }

  if (error.error && typeof error.error === 'object') {
    const body = error.error as Record<string, unknown>;
    for (const key of ['message', 'detail', 'title']) {
      const value = body[key];
      if (typeof value === 'string' && value.trim()) {
        return value;
      }
    }
  }

  switch (error.status) {
    case 0:
      return 'Cannot reach the API. Make sure the backend is running and its HTTPS development certificate is trusted.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested item could not be found.';
    case 500:
      return 'The server encountered an error. Please try again.';
    default:
      return fallback;
  }
}
