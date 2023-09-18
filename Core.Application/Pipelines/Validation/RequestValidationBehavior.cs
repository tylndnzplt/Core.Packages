using Core.CrossCuttingConcerns.Exceptions.Types;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValidationException = Core.CrossCuttingConcerns.Exceptions.Types.ValidationException;

namespace Core.Application.Pipelines.Validation;

public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> //IPipelineBehavior-->Mediatr'dan gelir.
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public RequestValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }


    /// <summary>
    /// Bizim bunu uyguladığımız her request ve response için eğer bir validor varsa bu handle'ı çalıştır.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ValidationContext<object> context = new(request);//object olmasının sebebi validation'a konu olabilecek her şey

        IEnumerable<ValidationExceptionModel> errors = _validators
            .Select(validator => validator.Validate(context)) //her bir validator için validate et
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .GroupBy(
                keySelector: p=>p.PropertyName,
                resultSelector:(propertyName,errors) => 
                    new ValidationExceptionModel { Property = propertyName, Errors = errors.Select(e => e.ErrorMessage) }
            ).ToList();

        //hata varsa
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }

        TResponse response = await next(); // her şey yolunda çalışabilirsin

        return response;
    }
}
