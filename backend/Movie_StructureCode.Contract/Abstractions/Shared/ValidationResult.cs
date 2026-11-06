namespace Movie_StructureCode.Contract.Abstractions.Shared
{
    public class ValidationResult : Result, IValidationResult
    {
        private ValidationResult(Error[] errors): base(false, IValidationResult.ValidationError)
            => Errors = errors;


        public Error[] Errors { get; }

        Error[] IValidationResult.Errors => throw new NotImplementedException();

        public static ValidationResult WithErrors(Error[] errors) => new(errors);
    }
}
