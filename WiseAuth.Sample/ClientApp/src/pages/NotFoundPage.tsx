import { Link } from 'react-router-dom'

// The server serves this SPA shell for any path that isn't a static asset or
// under /api, so a genuinely bad URL (typo, stale link) lands here rather
// than getting a server-rendered 404 - the client owns that decision instead.
export function NotFoundPage() {
  return (
    <div className="p-8 text-center">
      <p>Page not found.</p>
      <Link className="text-blue-600 hover:underline" to="/products">
        Back to Products
      </Link>
    </div>
  )
}
