using FluentValidation;
using MediatR;

namespace Movie_StructureCode.Application.Behaviors
{
    public sealed class ValidationDefaultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationDefaultBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;
        

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if(!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var errorsDictionary = _validators
                .Select(v => v.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(error => error != null)
                .GroupBy(x => new { x.PropertyName, x.ErrorMessage })
                .Select(x => x.FirstOrDefault()).ToList();

            if(errorsDictionary.Any())
                throw new ValidationException(errorsDictionary);

            return await next();
        }
    }
}
