namespace Banking.Application.Utilities;

/// <summary>
/// Normalizes reference strings for idempotency.
/// </summary>
public sealed class ReferenceNormalizer : IReferenceNormalizer
{
    public string Normalize(string reference)
    {
        return reference.Trim().ToUpperInvariant();
    }
}
