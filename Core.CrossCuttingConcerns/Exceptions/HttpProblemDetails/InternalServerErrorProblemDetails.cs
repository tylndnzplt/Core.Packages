using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Core.CrossCuttingConcerns.Exceptions.HttpProblemDetails;

/// <summary>
/// Öngrülemeyen server exceptionları için.
/// </summary>
public class InternalServerErrorProblemDetails : ProblemDetails
{
    public InternalServerErrorProblemDetails(string detail)
    {
        Title = "Internal Server Error";
        Detail = detail;
        Status = StatusCodes.Status500InternalServerError;//ilgli json bilgisi zaten çağırdığımız yerde veriyoruz
        Type = "http://example.com/probs/internal"; //bu bağımlı yapmadı mı ??
    }
}

