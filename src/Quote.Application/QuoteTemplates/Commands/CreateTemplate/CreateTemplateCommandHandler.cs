using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.QuoteTemplates.Commands.CreateTemplate;

public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, Result<CreateTemplateResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTemplateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateTemplateResponse>> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateTemplateResponse>.Failure("User not authenticated");
        }

        // Verify user is a tradie
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null || user.UserType != UserType.Tradie)
        {
            return Result<CreateTemplateResponse>.Failure("Only tradies can create quote templates");
        }

        // Verify trade category exists
        var categoryExists = await _context.TradeCategories
            .AnyAsync(c => c.Id == request.TradeCategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return Result<CreateTemplateResponse>.Failure("Invalid trade category");
        }

        var template = new QuoteTemplate
        {
            Id = Guid.NewGuid(),
            TradieId = _currentUser.UserId.Value,
            TradeCategoryId = request.TradeCategoryId,
            Name = request.Name,
            Description = request.Description,
            DefaultLabourCost = request.DefaultLabourCost,
            DefaultMaterialsCost = request.DefaultMaterialsCost,
            DefaultDurationHours = request.DefaultDurationHours,
            DefaultNotes = request.DefaultNotes,
            IsActive = true,
            UsageCount = 0
        };

        // Add materials if provided
        if (request.Materials != null && request.Materials.Any())
        {
            foreach (var material in request.Materials)
            {
                template.Materials.Add(new QuoteTemplateMaterial
                {
                    Id = Guid.NewGuid(),
                    TemplateId = template.Id,
                    ProductName = material.ProductName,
                    Quantity = material.Quantity,
                    Unit = material.Unit,
                    EstimatedUnitPrice = material.EstimatedUnitPrice
                });
            }
        }

        _context.QuoteTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateTemplateResponse>.Success(new CreateTemplateResponse
        {
            TemplateId = template.Id,
            Name = template.Name
        });
    }
}
