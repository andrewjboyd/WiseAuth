export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message)
  }
}

// Thrown for 400 responses. Every validation/business-rule failure in this API
// (ProductEndpoints' field checks, the self-edit and escalation guards in
// User/RoleEndpoints, ASP.NET Identity's own CreateAsync/SetUserNameAsync
// errors, ...) is surfaced as a JSON array of human-readable messages, so this
// is the one shape callers ever need to unwrap for inline field-error display.
export class ValidationError extends ApiError {
  constructor(public errors: string[]) {
    super(400, errors.join(' '))
  }
}

// For calls to protected resource endpoints (e.g. /products). Auth endpoints
// (/auth/login, /auth/me, /auth/logout) use plain fetch instead - a 401 there
// is an expected outcome, not a session that needs redirecting away from.
//
// Throws for every non-ok response instead of returning it, so callers never
// need to remember to check response.ok themselves - a check that's easy to
// forget on read-only call sites (and had been, more than once).
export async function apiFetch(input: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(input, {
    ...init,
    credentials: 'same-origin',
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (response.status === 401) {
    window.location.assign('/login')
    throw new ApiError(401, 'Not authenticated')
  }

  if (response.status === 403) {
    throw new ApiError(403, 'You do not have permission to do that')
  }

  if (response.status === 400) {
    throw new ValidationError(await response.json())
  }

  if (!response.ok) {
    throw new ApiError(response.status, `Request failed with status ${response.status}`)
  }

  return response
}
