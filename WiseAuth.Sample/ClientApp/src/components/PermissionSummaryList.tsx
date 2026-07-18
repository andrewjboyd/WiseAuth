import type { PermissionsSchema } from './PermissionEditor'

interface PermissionSummaryListProps {
  schema: PermissionsSchema
  value: Record<string, number>
}

// Read-only rendering of the bits an entity actually holds, against the schema's
// names - used where showing held permissions shouldn't also offer to edit them
// (e.g. the Users list, where editing lives on the user detail page instead).
export function PermissionSummaryList({ schema, value }: PermissionSummaryListProps) {
  return (
    <div className="flex flex-wrap gap-3 text-sm text-gray-700">
      {Object.entries(schema).map(([claimType, details]) => {
        const bitmask = value[claimType] ?? 0
        const held = details.filter((detail) => (bitmask & detail.id) !== 0)
        return (
          <div key={claimType}>
            <strong>{claimType}: </strong>
            {held.length > 0 ? held.map((detail) => detail.name).join(', ') : <span className="text-gray-400">none</span>}
          </div>
        )
      })}
    </div>
  )
}
