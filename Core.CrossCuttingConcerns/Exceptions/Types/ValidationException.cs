using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.CrossCuttingConcerns.Exceptions.Types;

public class ValidationException:Exception
{
    public IEnumerable<ValidationExceptionModel> Errors { get; }

    public ValidationException()
      : base()
    {
        Errors = Array.Empty<ValidationExceptionModel>();
    }

    public ValidationException(string? message)
        : base(message)
    {
        Errors = Array.Empty<ValidationExceptionModel>();
    }

    public ValidationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        Errors = Array.Empty<ValidationExceptionModel>();
    }

    /// <summary>
    /// Hataları olmuş olabilir doldur.
    /// </summary>
    /// <param name="errors"></param>
    public ValidationException(IEnumerable<ValidationExceptionModel> errors)
        : base(BuildErrorMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Hatayı açıkla
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    private static string BuildErrorMessage(IEnumerable<ValidationExceptionModel> errors)
    {
        IEnumerable<string> arr = errors.Select(
            x => $"{Environment.NewLine} -- {x.Property}: {string.Join(Environment.NewLine, values: x.Errors ?? Array.Empty<string>())}"
        );
        return $"Validation failed: {string.Join(string.Empty, arr)}";
    }
}

public class ValidationExceptionModel
{
    /// <summary>
    /// hangi alan? name gibi..
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// hangi hatalar?
    /// </summary>
    public IEnumerable<string> Errors { get; set; }
}
