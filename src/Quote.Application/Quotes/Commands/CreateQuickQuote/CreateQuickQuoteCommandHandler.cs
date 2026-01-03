using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.Quotes.Commands.CreateQuickQuote;

public class CreateQuickQuoteCommandHandler : IRequestHandler<CreateQuickQuoteCommand, Result<CreateQuickQuoteResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateQuickQuoteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateQuickQuoteResponse>> Handle(CreateQuickQuoteCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateQuickQuoteResponse>.Failure("User not authenticated");
        }

        // Verify user is a tradie
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null || user.UserType != UserType.Tradie)
        {
            return Result<CreateQuickQuoteResponse>.Failure("Only tradies can submit quotes");
        }

        // Verify job exists and is open for quotes
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return Result<CreateQuickQuoteResponse>.Failure("Job not found");
        }

        if (job.Status != JobStatus.Open && job.Status != JobStatus.Quoted)
        {
            return Result<CreateQuickQuoteResponse>.Failure("This job is not accepting quotes");
        }

        // Check if tradie already quoted this job
        var existingQuote = await _context.Quotes
            .AnyAsync(q => q.JobId == request.JobId && q.TradieId == _currentUser.UserId, cancellationToken);

        if (existingQuote)
        {
            return Result<CreateQuickQuoteResponse>.Failure("You have already submitted a quote for this job");
        }

        // If using a template, increment usage count
        if (request.TemplateId.HasValue)
        {
            var template = await _context.QuoteTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.TradieId == _currentUser.UserId, cancellationToken);

            if (template != null)
            {
                template.UsageCount++;
            }
        }

        var materialsCost = request.MaterialsCost ?? 0;
        var quote = new JobQuote
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            TradieId = _currentUser.UserId.Value,
            Status = QuoteStatus.Pending,
            LabourCost = request.LabourCost,
            MaterialsCost = materialsCost,
            EstimatedDurationHours = request.EstimatedDurationHours,
            ProposedStartDate = request.ProposedStartDate,
            Notes = request.Notes,
            ValidUntil = DateTime.UtcNow.AddDays(14), // 2 weeks validity
            TemplateId = request.TemplateId,
            DepositRequired = request.DepositRequired,
            RequiredDepositPercentage = request.DepositPercentage,
            RequiredDepositAmount = request.DepositRequired && request.DepositPercentage.HasValue
                ? (request.LabourCost + materialsCost) * (request.DepositPercentage.Value / 100)
                : null
        };

        // Handle material items
        var materialsList = new List<QuoteMaterial>();

        // Option 1: Load from existing bundle
        if (request.MaterialBundleId.HasValue)
        {
            var bundle = await _context.MaterialBundles
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == request.MaterialBundleId && b.TradieId == _currentUser.UserId, cancellationToken);

            if (bundle != null)
            {
                foreach (var item in bundle.Items.OrderBy(i => i.SortOrder))
                {
                    materialsList.Add(new QuoteMaterial
                    {
                        Id = Guid.NewGuid(),
                        QuoteId = quote.Id,
                        ProductName = item.ProductName,
                        SupplierName = item.SupplierName,
                        ProductUrl = item.ProductUrl,
                        Quantity = item.DefaultQuantity,
                        Unit = item.Unit,
                        UnitPrice = item.EstimatedUnitPrice
                    });
                }

                // Increment bundle usage count
                bundle.UsageCount++;
            }
        }
        // Option 2: Use materials from request
        else if (request.Materials != null && request.Materials.Any())
        {
            foreach (var item in request.Materials)
            {
                materialsList.Add(new QuoteMaterial
                {
                    Id = Guid.NewGuid(),
                    QuoteId = quote.Id,
                    ProductName = item.ProductName,
                    SupplierName = item.SupplierName,
                    ProductUrl = item.ProductUrl,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice
                });
            }
        }

        // Add materials to quote
        if (materialsList.Any())
        {
            quote.Materials = materialsList;
            // Recalculate materials cost from line items
            quote.MaterialsCost = materialsList.Sum(m => m.Quantity * m.UnitPrice);
        }

        // Save materials as new bundle if requested
        if (request.SaveMaterialsAsBundle && !string.IsNullOrWhiteSpace(request.NewBundleName) && materialsList.Any())
        {
            var newBundle = new MaterialBundle
            {
                Id = Guid.NewGuid(),
                TradieId = _currentUser.UserId.Value,
                Name = request.NewBundleName,
                TradeCategoryId = job.TradeCategoryId,
                IsActive = true,
                UsageCount = 1
            };

            var sortOrder = 0;
            foreach (var material in materialsList)
            {
                newBundle.Items.Add(new MaterialBundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = newBundle.Id,
                    ProductName = material.ProductName,
                    SupplierName = material.SupplierName,
                    ProductUrl = material.ProductUrl,
                    DefaultQuantity = material.Quantity,
                    Unit = material.Unit,
                    EstimatedUnitPrice = material.UnitPrice,
                    SortOrder = sortOrder++
                });
            }

            _context.MaterialBundles.Add(newBundle);
        }

        _context.Quotes.Add(quote);

        // Update job status to Quoted if this is the first quote
        if (job.Status == JobStatus.Open)
        {
            job.Status = JobStatus.Quoted;
        }

        // Auto-create conversation between tradie and customer
        var existingConversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.JobId == job.Id &&
                c.Participants.Any(p => p.UserId == _currentUser.UserId) &&
                c.Participants.Any(p => p.UserId == job.CustomerId),
                cancellationToken);

        if (existingConversation == null)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                LastMessageAt = DateTime.UtcNow,
                Participants = new List<ConversationParticipant>
                {
                    new ConversationParticipant
                    {
                        UserId = _currentUser.UserId.Value,
                        JoinedAt = DateTime.UtcNow
                    },
                    new ConversationParticipant
                    {
                        UserId = job.CustomerId,
                        JoinedAt = DateTime.UtcNow
                    }
                },
                Messages = new List<Message>
                {
                    new Message
                    {
                        Id = Guid.NewGuid(),
                        SenderId = _currentUser.UserId.Value,
                        Content = $"I've submitted a quote for your job: {job.Title}",
                        SentAt = DateTime.UtcNow,
                        IsSystemMessage = true
                    }
                }
            };
            _context.Conversations.Add(conversation);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateQuickQuoteResponse>.Success(new CreateQuickQuoteResponse
        {
            QuoteId = quote.Id,
            JobId = quote.JobId,
            TotalCost = quote.TotalCost,
            Status = quote.Status.ToString(),
            ValidUntil = quote.ValidUntil
        });
    }
}
