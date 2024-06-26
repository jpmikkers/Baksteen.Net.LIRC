namespace LIRCTestUI.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;

public sealed class EndPointValidationAttribute : ValidationAttribute
{
    public EndPointValidationAttribute()
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is string p)
        {
            try
            {
                _ = ConvertToUri(p);
                return ValidationResult.Success;
            }
            catch(Exception ex)
            {
                return new ValidationResult(ex.Message);
            }

            //try
            //{
            //    if(Uri.TryCreate($"tcp://{value}", UriKind.Absolute, out Uri? uri))
            //    {
            //        if(uri.IsDefaultPort)
            //        {
            //            return new ValidationResult("port number is required");
            //        }

            //        return ValidationResult.Success;
            //    }
            //}
            //catch { }
        }

        return new ValidationResult("endpoint is not valid");
    }

    public static Uri ConvertToUri(string rawEndpoint)
    {
        if(Uri.TryCreate($"tcp://{rawEndpoint}", UriKind.Absolute, out Uri? uri))
        {
            if(uri.IsDefaultPort)
            {
                throw new ValidationException("port number is required");
            }
            return uri;
        }
        throw new ValidationException("invalid endpoint");
    }
}
