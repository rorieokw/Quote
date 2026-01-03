using FluentValidation;
using Quote.Domain.Enums;

namespace Quote.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.UserType)
            .IsInEnum().WithMessage("Invalid user type")
            .Must(x => x == UserType.Customer || x == UserType.Tradie)
            .WithMessage("User type must be Customer or Tradie");

        // ABN is optional but must be valid format if provided
        RuleFor(x => x.ABN)
            .Matches(@"^\d{11}$").WithMessage("ABN must be 11 digits")
            .When(x => !string.IsNullOrEmpty(x.ABN));
    }
}
