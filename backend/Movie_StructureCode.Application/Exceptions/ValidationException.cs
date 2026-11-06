using Movie_StructureCode.Domain.Exceptions;

namespace Movie_StructureCode.Application.Exceptions
{
    public sealed class ValidationException : DomainException
    {
        public ValidationException(IReadOnlyCollection<ValidationError> errors) 
            : base("Validation Failure","One or more validation errors occured")
        => Errors = errors;

        public IReadOnlyCollection<ValidationError> Errors { get; }
    }

    public record ValidationError(string PropertyName,string ErrorMessage);
}
