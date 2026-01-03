using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;

namespace Quote.Application.Jobs.Queries.GetJob;

public class GetJobQueryHandler : IRequestHandler<GetJobQuery, Result<JobDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetJobQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<JobDetailDto>> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var job = await _context.Jobs
            .Include(j => j.TradeCategory)
            .Include(j => j.Customer)
            .Include(j => j.Media)
            .Include(j => j.Quotes)
                .ThenInclude(q => q.Tradie)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return Result<JobDetailDto>.Failure("Job not found");
        }

        var dto = new JobDetailDto
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            Status = job.Status.ToString(),
            TradeCategory = new TradeCategoryDto
            {
                Id = job.TradeCategory.Id,
                Name = job.TradeCategory.Name,
                Icon = job.TradeCategory.Icon
            },
            TradeCategoryId = job.TradeCategoryId,
            BudgetMin = job.BudgetMin,
            BudgetMax = job.BudgetMax,
            PreferredStartDate = job.PreferredStartDate,
            PreferredEndDate = job.PreferredEndDate,
            IsFlexibleDates = job.IsFlexibleDates,
            Location = new LocationDto
            {
                Latitude = job.Latitude,
                Longitude = job.Longitude,
                SuburbName = job.SuburbName,
                State = job.State.ToString(),
                Postcode = job.Postcode
            },
            PropertyType = job.PropertyType.ToString(),
            Customer = new CustomerDto
            {
                Id = job.Customer.Id,
                FirstName = job.Customer.FirstName,
                ProfilePhotoUrl = job.Customer.ProfilePhotoUrl
            },
            Media = job.Media.OrderBy(m => m.SortOrder).Select(m => new JobMediaDto
            {
                Id = m.Id,
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType.ToString(),
                Caption = m.Caption,
                ThumbnailUrl = m.ThumbnailUrl
            }).ToList(),
            Quotes = job.Quotes.Select(q => new QuoteSummaryDto
            {
                Id = q.Id,
                TradieId = q.TradieId,
                TradieName = q.Tradie.FirstName,
                TotalCost = q.TotalCost,
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt
            }).ToList(),
            CreatedAt = job.CreatedAt
        };

        return Result<JobDetailDto>.Success(dto);
    }
}
