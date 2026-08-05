using FluentValidation;
using OppSignal.Application.Auth;
using OppSignal.Application.Profiles;

namespace OppSignal.Api.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.CompanyName).MaximumLength(200);
        // Clickwrap assent — the affirmative act that makes the Terms enforceable.
        RuleFor(x => x.AcceptedTerms).Equal(true)
            .WithMessage("You must accept the Terms of Service and Privacy Policy to create an account.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Choose a password different from your current one.");
    }
}

public sealed class ProfileInputValidator : AbstractValidator<ProfileInput>
{
    public ProfileInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Naics).Must(l => l.Count <= 100).WithMessage("Too many NAICS codes.");
        RuleFor(x => x.Psc).Must(l => l.Count <= 100).WithMessage("Too many PSC codes.");
        RuleFor(x => x.Keywords).Must(l => l.Count <= 100).WithMessage("Too many keywords.");
        RuleFor(x => x.AgencyPaths).Must(l => l.Count <= 100).WithMessage("Too many agencies.");
        RuleFor(x => x.States).Must(l => l.Count <= 60).WithMessage("Too many states.");
        // Real code formats only: NAICS is 2-6 digits, PSC is 1-4 alphanumerics.
        // Garbage here isn't just noise — every stored code costs a daily
        // USAspending query in the Recompete Radar ingest.
        RuleForEach(x => x.Naics).Matches("^[0-9]{2,6}$").WithMessage("NAICS codes are 2-6 digits.");
        RuleForEach(x => x.Psc).Matches("^[A-Za-z0-9]{1,6}$").WithMessage("PSC codes are 1-6 letters/digits.");
        RuleForEach(x => x.Keywords).MaximumLength(100);
        RuleForEach(x => x.AgencyPaths).MaximumLength(200);
        RuleForEach(x => x.States).Matches("^[A-Za-z]{2}$").WithMessage("States must be 2-letter codes.");
        RuleFor(x => x).Must(HasAtLeastOneCriterion)
            .WithMessage("A profile needs at least one NAICS, PSC, or keyword to match on.");
    }

    private static bool HasAtLeastOneCriterion(ProfileInput p)
        => p.Naics.Count > 0 || p.Psc.Count > 0 || p.Keywords.Count > 0;
}
