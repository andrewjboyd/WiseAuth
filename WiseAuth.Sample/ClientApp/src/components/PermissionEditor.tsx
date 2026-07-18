export interface EndpointDetail {
  id: number
  name: string
}

export type PermissionsSchema = Record<string, EndpointDetail[]>

interface PermissionEditorProps {
  schema: PermissionsSchema
  value: Record<string, number>
  onChange: (claimType: string, bitmask: number) => void
}

// Dynamic, schema-driven so it never hardcodes a claim type - it renders one
// checkbox group per permission enum registered with AddWiseAuth<T>() (fetched
// from GET /api/auth/permissions-schema), so a new enum shows up here for free.
export function PermissionEditor({ schema, value, onChange }: PermissionEditorProps) {
  return (
    <div className="flex flex-wrap gap-3">
      {Object.entries(schema).map(([claimType, details]) => {
        const bitmask = value[claimType] ?? 0
        return (
          <fieldset key={claimType} className="rounded-md border border-gray-200 px-2.5 py-1.5">
            <legend className="px-1 text-xs uppercase text-gray-500">{claimType}</legend>
            {details.map((detail) => (
              <label key={detail.id} className="flex items-center gap-1.5 whitespace-nowrap text-sm text-gray-700">
                <input
                  type="checkbox"
                  className="h-3.5 w-3.5"
                  checked={(BigInt(bitmask) & BigInt(detail.id)) !== 0n}
                  onChange={(e) => {
                    // BigInt, not the `&`/`|`/`~` operators - those coerce to 32-bit
                    // signed ints and would silently corrupt any bit at or above 2^31,
                    // which WiseAuth's power-of-two enums otherwise fully support.
                    const bit = BigInt(detail.id)
                    const next = e.target.checked ? BigInt(bitmask) | bit : BigInt(bitmask) & ~bit
                    onChange(claimType, Number(next))
                  }}
                />
                {detail.name}
              </label>
            ))}
          </fieldset>
        )
      })}
    </div>
  )
}
