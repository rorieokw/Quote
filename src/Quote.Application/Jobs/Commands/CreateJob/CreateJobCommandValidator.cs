using FluentValidation;

namespace Quote.Application.Jobs.Commands.CreateJob;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.TradeCategoryId)
            .NotEmpty().WithMessage("Trade category is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.BudgetMax)
            .GreaterThanOrEqualTo(x => x.BudgetMin)
            .When(x => x.BudgetMin.HasValue && x.BudgetMax.HasValue)
            .WithMessage("Maximum budget must be greater than or equal to minimum budget");

        RuleFor(x => x.PreferredEndDate)
            .GreaterThanOrEqualTo(x => x.PreferredStartDate)
            .When(x => x.PreferredStartDate.HasValue && x.PreferredEndDate.HasValue)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.SuburbName)
            .NotEmpty().WithMessage("Suburb is required")
            .MaximumLength(100).WithMessage("Suburb cannot exceed 100 characters");

        RuleFor(x => x.Postcode)
            .NotEmpty().WithMessage("Postcode is required")
            .Matches(@"^\d{4}$").WithMessage("Postcode must be 4 digits");

        RuleFor(x => x.State)
            .IsInEnum().WithMessage("Invalid state");

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("Invalid property type");
    }
}
