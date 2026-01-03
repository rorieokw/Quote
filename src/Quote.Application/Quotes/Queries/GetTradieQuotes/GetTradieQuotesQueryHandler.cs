using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Quotes.Queries.GetTradieQuotes;

public class GetTradieQuotesQueryHandler : IRequestHandler<GetTradieQuotesQuery, Result<GetTradieQuotesResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTradieQuotesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<GetTradieQuotesResponse>> Handle(GetTradieQuotesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<GetTradieQuotesResponse>.Failure("User not authenticated");
        }

        var query = _context.Quotes
            .Include(q => q.Job)
                .ThenInclude(j => j.Customer)
            .Include(q => q.Job)
                .ThenInclude(j => j.Quotes)
            .Where(q => q.TradieId == _currentUser.UserId);

        // Apply status filter if provided
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<QuoteStatus>(request.Status, out var status))
        {
            query = query.Where(q => q.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var quotes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var quoteDtos = quotes.Select(q => new QuoteWithStatusDto
        {
            QuoteId = q.Id,
            JobId = q.JobId,
            JobTitle = q.Job.Title,
            JobSuburb = q.Job.SuburbName,
            CustomerName = q.Job.Customer.FullName,
            Status = q.Status.ToString(),
            TotalCost = q.TotalCost,
            ViewCount = q.ViewCount,
            FirstViewedAt = q.FirstViewedAt,
            LastViewedAt = q.LastViewedAt,
            CompetingQuotes = q.Job.Quotes.Count - 1, // Exclude own quote
            IsWinningQuote = q.Job.AcceptedQuoteId == q.Id,
            IsJobStillOpen = q.Job.Status == JobStatus.Open || q.Job.Status == JobStatus.Quoted,
            CreatedAt = q.CreatedAt,
            ValidUntil = q.ValidUntil
        }).ToList();

        return Result<GetTradieQuotesResponse>.Success(new GetTradieQuotesResponse
        {
            Quotes = quoteDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
