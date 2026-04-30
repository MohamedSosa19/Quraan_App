using FluentValidation;

namespace Quraan.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly string[] s_languages = ["ar", "en"];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(256);
        RuleFor(x => x.DisplayName)
            .MaximumLength(128)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        RuleFor(x => x.PreferredLanguage)
            .Must(v => v is null || s_languages.Contains(v))
            .WithMessage("preferredLanguage must be 'ar' or 'en'.");
    }
}
