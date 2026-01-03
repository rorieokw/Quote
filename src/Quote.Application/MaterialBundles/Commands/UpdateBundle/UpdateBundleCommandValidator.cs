using FluentValidation;

namespace Quote.Application.MaterialBundles.Commands.UpdateBundle;

public class UpdateBundleCommandValidator : AbstractValidator<UpdateBundleCommand>
{
    public UpdateBundleCommandValidator()
    {
        RuleFor(x => x.BundleId)
            .NotEmpty().WithMessage("Bundle ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Bundle name is required")
            .MaximumLength(100).WithMessage("Bundle name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one material item is required");

        RuleForEach(x => x.Items).SetValidator(new UpdateBundleItemValidator());
    }
}

public class UpdateBundleItemValidator : AbstractValidator<UpdateBundleItemCommand>
{
    public UpdateBundleItemValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.SupplierName)
            .MaximumLength(200).WithMessage("Supplier name cannot exceed 200 characters");

        RuleFor(x => x.ProductUrl)
            .MaximumLength(500).WithMessage("Product URL cannot exceed 500 characters");

        RuleFor(x => x.DefaultQuantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit cannot exceed 50 characters");

        RuleFor(x => x.EstimatedUnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");
    }
}
