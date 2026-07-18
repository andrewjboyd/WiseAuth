import type { PermissionsSchema } from './PermissionEditor'

interface PermissionSchemaTableProps {
  schema: PermissionsSchema
}

// Read-only catalog of every permission bit registered with AddWiseAuth<T>() -
// the Access Controls tab's whole purpose, not an editable view.
export function PermissionSchemaTable({ schema }: PermissionSchemaTableProps) {
  return (
    <div className="flex flex-wrap gap-3">
      {Object.entries(schema).map(([claimType, details]) => (
        <fieldset key={claimType} className="rounded-md border border-gray-200 px-2.5 py-1.5">
          <legend className="px-1 text-xs uppercase text-gray-500">{claimType}</legend>
          <ul className="m-0 list-disc pl-5 text-sm text-gray-700">
            {details.map((detail) => (
              <li key={detail.id}>
                {detail.name} <code className="text-xs text-gray-500">({detail.id})</code>
              </li>
            ))}
          </ul>
        </fieldset>
      ))}
    </div>
  )
}
