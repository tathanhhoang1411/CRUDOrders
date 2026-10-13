using FluentValidation;
using MediatR;
using CRUDOrders.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior tu dong chay FluentValidation truoc khi Handler xu ly.
/// Neu co loi validation, tra ve Result&lt;T&gt;.Error ma khong goi den Handler.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join(", ", failures.Select(e => e.ErrorMessage));
        _logger.LogWarning("[Validation] {RequestType} failed: {Errors}", typeof(TRequest).Name, errorMessage);

        // Tra ve Result<T>.Error neu TResponse la Result<T>
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var dataType = responseType.GetGenericArguments()[0];
            var errorMethod = responseType.GetMethod("Error",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (errorMethod != null)
            {
                var result = errorMethod.Invoke(null, new object[] { "VALIDATION_ERROR", errorMessage });
                return (TResponse)result!;
            }
        }

        throw new ValidationException(failures);
    }
}
