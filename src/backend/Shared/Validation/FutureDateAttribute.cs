using System.ComponentModel.DataAnnotations;

namespace Shared.Validation;

public class FutureDateAttribute: ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTimeOffset dateValue)
        {
            return dateValue > DateTimeOffset.UtcNow
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be in the future");
        }

        return ValidationResult.Success;
    }
}
