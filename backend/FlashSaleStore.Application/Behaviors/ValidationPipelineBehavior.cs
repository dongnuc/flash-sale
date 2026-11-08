using FlashSaleStore.Contract.Abstractions.Shared;
using FluentValidation;
using MediatR;

namespace FlashSaleStore.Application.Behaviors;

public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(request, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => new Error(failure.PropertyName, failure.ErrorMessage))
            .Distinct()
            .ToArray();

        if (errors.Length > 0)
        {
            return CreateValidationResult<TResponse>(errors);
        }

        return await next();
    }

    private static TResponseType CreateValidationResult<TResponseType>(Error[] errors)
        where TResponseType : Result
    {
        if (typeof(TResponseType) == typeof(Result))
        {
            return (TResponseType)(object)ValidationResult.WithErrors(errors);
        }

        var responseType = typeof(TResponseType);
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            throw new InvalidOperationException($"Unsupported validation response type: {responseType.FullName}.");
        }

        var valueType = responseType.GetGenericArguments()[0];
        var validationResultType = typeof(ValidationResult<>).MakeGenericType(valueType);
        var factory = validationResultType.GetMethod(nameof(ValidationResult<object>.WithErrors))
            ?? throw new InvalidOperationException("Could not create a validation result for the request.");

        return (TResponseType)factory.Invoke(null, [errors])!;
    }
}
