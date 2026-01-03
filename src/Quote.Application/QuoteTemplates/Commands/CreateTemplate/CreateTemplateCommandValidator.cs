using FluentValidation;

namespace Quote.Application.QuoteTemplates.Commands.CreateTemplate;

public class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(100).WithMessage("Template name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.TradeCategoryId)
            .NotEmpty().WithMessage("Trade category is required");

        RuleFor(x => x.DefaultLabourCost)
            .GreaterThan(0).WithMessage("Default labour cost must be greater than 0");

        RuleFor(x => x.DefaultMaterialsCost)
            .GreaterThanOrEqualTo(0).When(x => x.DefaultMaterialsCost.HasValue)
            .WithMessage("Default materials cost cannot be negative");

        RuleFor(x => x.DefaultDurationHours)
            .GreaterThan(0).WithMessage("Default duration must be at least 1 hour");

        RuleFor(x => x.DefaultNotes)
            .MaximumLength(2000).WithMessage("Default notes cannot exceed 2000 characters");

        RuleForEach(x => x.Materials).SetValidator(new TemplateMaterialValidator());
    }
}

public class TemplateMaterialValidator : AbstractValidator<CreateTemplateMaterialCommand>
{
    public TemplateMaterialValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Material name is required")
            .MaximumLength(200).WithMessage("Material name cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit cannot exceed 50 characters");

        RuleFor(x => x.EstimatedUnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
    }
}
