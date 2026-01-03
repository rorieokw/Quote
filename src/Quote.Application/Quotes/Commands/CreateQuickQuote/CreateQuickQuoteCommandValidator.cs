using FluentValidation;

namespace Quote.Application.Quotes.Commands.CreateQuickQuote;

public class CreateQuickQuoteCommandValidator : AbstractValidator<CreateQuickQuoteCommand>
{
    public CreateQuickQuoteCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required");

        RuleFor(x => x.LabourCost)
            .GreaterThan(0).WithMessage("Labour cost must be greater than 0");

        RuleFor(x => x.MaterialsCost)
            .GreaterThanOrEqualTo(0).When(x => x.MaterialsCost.HasValue)
            .WithMessage("Materials cost cannot be negative");

        RuleFor(x => x.EstimatedDurationHours)
            .GreaterThan(0).WithMessage("Estimated duration must be at least 1 hour")
            .LessThanOrEqualTo(1000).WithMessage("Estimated duration seems unrealistic");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters");

        RuleFor(x => x.DepositPercentage)
            .InclusiveBetween(1, 100).When(x => x.DepositRequired && x.DepositPercentage.HasValue)
            .WithMessage("Deposit percentage must be between 1% and 100%");

        RuleFor(x => x.DepositPercentage)
            .NotNull().When(x => x.DepositRequired)
            .WithMessage("Deposit percentage is required when deposit is required");
    }
}
