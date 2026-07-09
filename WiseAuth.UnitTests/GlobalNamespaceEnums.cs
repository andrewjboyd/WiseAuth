// Intentionally declared with no namespace so typeof(T).FullName contains no '.',
// exercising the lastDotIdx == -1 branch of WiseAuthMetadata.DetermineClaimType.

// ReSharper disable once CheckNamespace

internal enum GlobalEnum
{
    Value = 1,
}

internal abstract class GlobalController
{
    public enum Permissions
    {
        View = 1,
    }
}
