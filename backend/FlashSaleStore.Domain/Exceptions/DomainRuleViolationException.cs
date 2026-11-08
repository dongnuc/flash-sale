namespace FlashSaleStore.Domain.Exceptions;

public enum DomainFailureKind
{
    Validation,
    Conflict,
    NotFound,
    Forbidden
}

/// <summary>Represents an expected domain invariant or business-rule violation.</summary>
public sealed class DomainRuleViolationException : DomainException
{
    public string Code { get; }
    public DomainFailureKind Kind { get; }

    public DomainRuleViolationException(
        string code,
        string message,
        DomainFailureKind kind = DomainFailureKind.Validation)
        : base(code, message)
    {
        Code = code;
        Kind = kind;
    }
}
