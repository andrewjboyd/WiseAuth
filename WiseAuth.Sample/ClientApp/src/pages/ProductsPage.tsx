import { useEffect, useState, type FormEvent } from 'react'
import { NavHeader } from '../components/NavHeader'
import { ApiError, apiFetch } from '../lib/api'
import { useAuth } from '../lib/AuthContext'
import { bannerClass, buttonClass, classNames, hintClass, inputClass, lockedTitle, pageShellClass, tdClass, thClass } from '../lib/uiHelpers'

interface Product {
  id: number
  sku: string
  name: string
  price: number
  quantity: number
  createdUtc: string
}

interface Banner {
  kind: 'locked' | 'error'
  text: string
}

const emptyForm = { sku: '', name: '', price: '', quantity: '' }

export function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [form, setForm] = useState(emptyForm)
  const [banner, setBanner] = useState<Banner | null>(null)
  const { hasPermission } = useAuth()

  const canWrite = hasPermission('products', 2)
  const canDelete = hasPermission('products', 4)
  const canExport = hasPermission('products', 8)

  useEffect(() => {
    void loadProducts()
  }, [])

  async function loadProducts() {
    try {
      const response = await apiFetch('/api/products')
      setProducts(await response.json())
    } catch (err) {
      reportError(err, 'Read')
    }
  }

  function reportError(err: unknown, permission: string) {
    if (err instanceof ApiError && err.status === 403) {
      setBanner({
        kind: 'locked',
        text: `🔒 Blocked — your account doesn't have the ${permission} permission for Products. The server returned 403.`,
      })
      return
    }
    setBanner({ kind: 'error', text: err instanceof ApiError ? err.message : 'Something went wrong' })
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    setBanner(null)
    try {
      await apiFetch('/api/products', {
        method: 'POST',
        body: JSON.stringify({
          sku: form.sku,
          name: form.name,
          price: Number(form.price),
          quantity: Number(form.quantity),
        }),
      })
      setForm(emptyForm)
      await loadProducts()
    } catch (err) {
      reportError(err, 'Write')
    }
  }

  async function handleDelete(id: number) {
    setBanner(null)
    try {
      await apiFetch(`/api/products/${id}`, { method: 'DELETE' })
      await loadProducts()
    } catch (err) {
      reportError(err, 'Delete')
    }
  }

  async function handleExport() {
    setBanner(null)
    try {
      const response = await apiFetch('/api/products/export')
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'products.csv'
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      reportError(err, 'Export')
    }
  }

  return (
    <div className={pageShellClass}>
      <NavHeader title="Products" />

      {banner && <p className={bannerClass(banner.kind)}>{banner.text}</p>}

      <table className="w-full border-collapse overflow-hidden rounded-lg bg-white">
        <thead>
          <tr>
            <th className={thClass}>SKU</th>
            <th className={thClass}>Name</th>
            <th className={thClass}>Price</th>
            <th className={thClass}>Quantity</th>
            <th className={thClass} />
          </tr>
        </thead>
        <tbody>
          {products.map((product) => (
            <tr key={product.id}>
              <td className={tdClass}>{product.sku}</td>
              <td className={tdClass}>{product.name}</td>
              <td className={tdClass}>{product.price.toFixed(2)}</td>
              <td className={tdClass}>{product.quantity}</td>
              <td className={tdClass}>
                <button
                  className={buttonClass(!canDelete)}
                  title={lockedTitle(canDelete, 'Delete')}
                  onClick={() => handleDelete(product.id)}
                >
                  {!canDelete && '🔒 '}Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <button
        className={classNames('my-4', buttonClass(!canExport, 'secondary'))}
        title={lockedTitle(canExport, 'Export')}
        onClick={handleExport}
      >
        {!canExport && '🔒 '}Export CSV
      </button>

      <form className="flex flex-wrap items-center gap-2 rounded-lg bg-white p-4" onSubmit={handleCreate}>
        <h2 className="mb-2 w-full text-base font-semibold">{!canWrite && '🔒 '}New product</h2>
        {!canWrite && (
          <p className={classNames(hintClass, 'w-full')}>
            Your account only has Read access — submitting this will be blocked by the server (403).
          </p>
        )}
        <input
          className={classNames(inputClass, 'min-w-[140px] flex-1')}
          placeholder="SKU"
          value={form.sku}
          onChange={(e) => setForm({ ...form, sku: e.target.value })}
          required
        />
        <input
          className={classNames(inputClass, 'min-w-[140px] flex-1')}
          placeholder="Name"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          required
        />
        <input
          className={classNames(inputClass, 'min-w-[140px] flex-1')}
          placeholder="Price"
          type="number"
          step="0.01"
          value={form.price}
          onChange={(e) => setForm({ ...form, price: e.target.value })}
          required
        />
        <input
          className={classNames(inputClass, 'min-w-[140px] flex-1')}
          placeholder="Quantity"
          type="number"
          value={form.quantity}
          onChange={(e) => setForm({ ...form, quantity: e.target.value })}
          required
        />
        <button type="submit" className={buttonClass(!canWrite)} title={lockedTitle(canWrite, 'Write')}>
          {!canWrite && '🔒 '}Create
        </button>
      </form>
    </div>
  )
}
