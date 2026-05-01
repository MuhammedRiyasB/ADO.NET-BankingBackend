namespace Banking.Application.Utilities;

/// <summary>
/// Normalizes reference strings for idempotency.
/// </summary>
public interface IReferenceNormalizer
{
    /// <summary>
    /// Normalizes a reference by trimming and converting to uppercase.
    /// </summary>
    string Normalize(string reference);
}
