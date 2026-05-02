namespace Banking.Application.Common;

public sealed class DuplicateResourceException : Exception
{
    public DuplicateResourceException(string resourceName, string fieldName, string fieldValue, Exception? innerException = null)
        : base($"A {resourceName} with {fieldName} '{fieldValue}' already exists.", innerException)
    {
        ResourceName = resourceName;
        FieldName = fieldName;
        FieldValue = fieldValue;
    }

    public string ResourceName { get; }
    public string FieldName { get; }
    public string FieldValue { get; }
}
