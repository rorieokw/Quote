using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;

namespace Quote.Application.QuoteTemplates.Queries.GetTemplates;

public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, Result<GetTemplatesResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTemplatesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<GetTemplatesResponse>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<GetTemplatesResponse>.Failure("User not authenticated");
        }

        var query = _context.QuoteTemplates
            .Include(t => t.TradeCategory)
            .Include(t => t.Materials)
            .Where(t => t.TradieId == _currentUser.UserId);

        if (!request.IncludeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        if (request.TradeCategoryId.HasValue)
        {
            query = query.Where(t => t.TradeCategoryId == request.TradeCategoryId);
        }

        var templates = await query
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var templateDtos = templates.Select(t => new TemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            TradeCategoryId = t.TradeCategoryId,
            TradeCategoryName = t.TradeCategory.Name,
            DefaultLabourCost = t.DefaultLabourCost,
            DefaultMaterialsCost = t.DefaultMaterialsCost,
            DefaultDurationHours = t.DefaultDurationHours,
            DefaultNotes = t.DefaultNotes,
            UsageCount = t.UsageCount,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            Materials = t.Materials.Select(m => new TemplateMaterialDto
            {
                Id = m.Id,
                ProductName = m.ProductName,
                Quantity = m.Quantity,
                Unit = m.Unit,
                EstimatedUnitPrice = m.EstimatedUnitPrice,
                TotalPrice = m.Quantity * m.EstimatedUnitPrice
            }).ToList()
        }).ToList();

        return Result<GetTemplatesResponse>.Success(new GetTemplatesResponse
        {
            Templates = templateDtos,
            TotalCount = templateDtos.Count
        });
    }
}
