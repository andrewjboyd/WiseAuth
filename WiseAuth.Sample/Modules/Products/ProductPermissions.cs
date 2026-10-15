using System.ComponentModel.DataAnnotations;

namespace WiseAuth.Sample.Modules.Products;

[ClaimType("products")]
[Flags]
public enum ProductPermissions
{
    [Display(Name = "View products", Description = "View the product catalog.")]
    Read = 1,

    [Display(Name = "Manage products", Description = "Create and update product records.")]
    Write = 2,

    [Display(Name = "Delete products", Description = "Permanently remove product records.")]
    Delete = 4,

    [Display(Name = "Export products", Description = "Download the product catalog as a CSV file.")]
    Export = 8,
}
