using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WiseAuth.Sample.Products;

namespace WiseAuth.Sample.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options),
      IDataProtectionKeyContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Backstop for ProductEndpoints' application-level duplicate-Sku check: two
        // concurrent requests can both pass that check before either has saved, so the
        // database constraint is what actually prevents the race from producing two
        // rows with the same Sku.
        builder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();
    }
}
