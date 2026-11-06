namespace Movie_StructureCode.Contract.Abstractions.Shared
{
    public sealed class ValidationResult<TValue> : Result<TValue>, IValidationResult
    {
        private ValidationResult( Error[] errors)
            : base(default, false, IValidationResult.ValidationError) 
            => Errors = errors;
        public Error[] Errors { get; }
        Error[] IValidationResult.Errors => throw new NotImplementedException();
        public static ValidationResult<TValue> WithErrors(Error[] errors) 
            => new( errors);
    }
    
}
