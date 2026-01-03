using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Result<CreateJobResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateJobCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateJobResponse>> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateJobResponse>.Failure("User not authenticated");
        }

        var categoryExists = await _context.TradeCategories
            .AnyAsync(c => c.Id == request.TradeCategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return Result<CreateJobResponse>.Failure("Invalid trade category");
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = _currentUser.UserId.Value,
            TradeCategoryId = request.TradeCategoryId,
            Title = request.Title,
            Description = request.Description,
            Status = request.PublishImmediately ? JobStatus.Open : JobStatus.Draft,
            BudgetMin = request.BudgetMin,
            BudgetMax = request.BudgetMax,
            PreferredStartDate = request.PreferredStartDate,
            PreferredEndDate = request.PreferredEndDate,
            IsFlexibleDates = request.IsFlexibleDates,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SuburbName = request.SuburbName,
            State = request.State,
            Postcode = request.Postcode,
            PropertyType = request.PropertyType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateJobResponse>.Success(new CreateJobResponse
        {
            JobId = job.Id,
            Title = job.Title,
            Status = job.Status.ToString()
        });
    }
}
