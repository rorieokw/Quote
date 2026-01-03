using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.RecurringJobs.Services;

public class RecurringJobService : IRecurringJobService
{
    private readonly IApplicationDbContext _context;

    public RecurringJobService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RecurringJobTemplate> CreateTemplateAsync(Guid customerId, CreateRecurringJobTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found");

        var tradeCategory = await _context.TradeCategories.FirstOrDefaultAsync(t => t.Id == request.TradeCategoryId, cancellationToken)
            ?? throw new InvalidOperationException("Trade category not found");

        if (request.TradieId.HasValue)
        {
            var tradie = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.TradieId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Tradie not found");
        }

        if (!Enum.TryParse<RecurrencePattern>(request.Pattern, true, out var pattern))
        {
            throw new InvalidOperationException("Invalid recurrence pattern");
        }

        var template = new RecurringJobTemplate
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TradieId = request.TradieId,
            TradeCategoryId = request.TradeCategoryId,
            Title = request.Title,
            Description = request.Description,
            Address = request.Address,
            SuburbName = request.SuburbName,
            PostCode = request.PostCode,
            State = request.State,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Pattern = pattern,
            CustomIntervalDays = request.CustomIntervalDays ?? 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxOccurrences = request.MaxOccurrences,
            OccurrencesGenerated = 0,
            EstimatedBudgetMin = request.EstimatedBudgetMin,
            EstimatedBudgetMax = request.EstimatedBudgetMax,
            IsActive = true,
            NextDueDate = request.StartDate,
            Notes = request.Notes,
            AutoAcceptFromTradie = request.AutoAcceptFromTradie
        };

        _context.RecurringJobTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task<RecurringJobTemplate?> UpdateTemplateAsync(Guid customerId, Guid templateId, UpdateRecurringJobTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CustomerId == customerId, cancellationToken);

        if (template == null)
            return null;

        if (!string.IsNullOrEmpty(request.Title))
            template.Title = request.Title;

        if (request.Description != null)
            template.Description = request.Description;

        if (!string.IsNullOrEmpty(request.Pattern) && Enum.TryParse<RecurrencePattern>(request.Pattern, true, out var pattern))
        {
            template.Pattern = pattern;
            // Recalculate next due date
            if (template.LastGeneratedAt.HasValue)
            {
                template.NextDueDate = CalculateNextDueDate(template.LastGeneratedAt.Value, pattern, request.CustomIntervalDays ?? template.CustomIntervalDays);
            }
        }

        if (request.CustomIntervalDays.HasValue)
            template.CustomIntervalDays = request.CustomIntervalDays.Value;

        if (request.EndDate.HasValue)
            template.EndDate = request.EndDate;

        if (request.MaxOccurrences.HasValue)
            template.MaxOccurrences = request.MaxOccurrences;

        if (request.EstimatedBudgetMin.HasValue)
            template.EstimatedBudgetMin = request.EstimatedBudgetMin;

        if (request.EstimatedBudgetMax.HasValue)
            template.EstimatedBudgetMax = request.EstimatedBudgetMax;

        if (request.Notes != null)
            template.Notes = request.Notes;

        if (request.AutoAcceptFromTradie.HasValue)
            template.AutoAcceptFromTradie = request.AutoAcceptFromTradie.Value;

        if (request.IsActive.HasValue)
            template.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task<RecurringJobTemplateDto?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .Include(t => t.Customer)
            .Include(t => t.Tradie)
            .Include(t => t.TradeCategory)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
            return null;

        return MapToDto(template);
    }

    public async Task<RecurringJobTemplatesResponse> GetTemplatesAsync(Guid userId, bool isTradie, int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.RecurringJobTemplates
            .Include(t => t.Customer)
            .Include(t => t.Tradie)
            .Include(t => t.TradeCategory)
            .AsQueryable();

        if (isTradie)
        {
            query = query.Where(t => t.TradieId == userId);
        }
        else
        {
            query = query.Where(t => t.CustomerId == userId);
        }

        if (activeOnly == true)
        {
            query = query.Where(t => t.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var templates = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var weekFromNow = now.AddDays(7);

        var allTemplates = isTradie
            ? await _context.RecurringJobTemplates.Where(t => t.TradieId == userId).ToListAsync(cancellationToken)
            : await _context.RecurringJobTemplates.Where(t => t.CustomerId == userId).ToListAsync(cancellationToken);

        var stats = new RecurringJobStats(
            TotalTemplates: allTemplates.Count,
            ActiveTemplates: allTemplates.Count(t => t.IsActive),
            UpcomingThisWeek: allTemplates.Count(t => t.IsActive && t.NextDueDate >= now && t.NextDueDate <= weekFromNow),
            TotalJobsGenerated: allTemplates.Sum(t => t.OccurrencesGenerated)
        );

        return new RecurringJobTemplatesResponse(
            Templates: templates.Select(t => new RecurringJobTemplateListItemDto(
                Id: t.Id,
                Title: t.Title,
                TradeCategoryName: t.TradeCategory.Name,
                TradieName: t.Tradie != null ? $"{t.Tradie.FirstName} {t.Tradie.LastName}" : null,
                Pattern: t.Pattern.ToString(),
                NextDueDate: t.NextDueDate,
                OccurrencesGenerated: t.OccurrencesGenerated,
                MaxOccurrences: t.MaxOccurrences,
                IsActive: t.IsActive,
                CreatedAt: t.CreatedAt
            )).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            Stats: stats
        );
    }

    public async Task<bool> DeactivateTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CustomerId == customerId, cancellationToken);

        if (template == null)
            return false;

        template.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ActivateTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.CustomerId == customerId, cancellationToken);

        if (template == null)
            return false;

        // Check if template can be reactivated
        if (template.EndDate.HasValue && template.EndDate.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Cannot activate a template that has already ended");
        }

        if (template.MaxOccurrences.HasValue && template.OccurrencesGenerated >= template.MaxOccurrences.Value)
        {
            throw new InvalidOperationException("Cannot activate a template that has reached maximum occurrences");
        }

        template.IsActive = true;

        // Recalculate next due date if needed
        if (template.NextDueDate < DateTime.UtcNow)
        {
            template.NextDueDate = CalculateNextDueDate(DateTime.UtcNow, template.Pattern, template.CustomIntervalDays);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<GenerateJobsResult> GenerateDueJobsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueTemplates = await _context.RecurringJobTemplates
            .Include(t => t.TradeCategory)
            .Where(t => t.IsActive
                && t.NextDueDate <= now
                && (t.EndDate == null || t.EndDate > now)
                && (t.MaxOccurrences == null || t.OccurrencesGenerated < t.MaxOccurrences))
            .ToListAsync(cancellationToken);

        var generatedJobIds = new List<Guid>();
        var errors = new List<string>();

        foreach (var template in dueTemplates)
        {
            try
            {
                var job = await GenerateJobFromTemplateAsync(template, cancellationToken);
                if (job != null)
                {
                    generatedJobIds.Add(job.Id);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Template {template.Id}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateJobsResult(
            JobsGenerated: generatedJobIds.Count,
            GeneratedJobIds: generatedJobIds,
            Errors: errors
        );
    }

    public async Task<GenerateJobsResult> GenerateJobsForTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .Include(t => t.TradeCategory)
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            return new GenerateJobsResult(0, new List<Guid>(), new List<string> { "Template not found" });
        }

        if (!template.IsActive)
        {
            return new GenerateJobsResult(0, new List<Guid>(), new List<string> { "Template is not active" });
        }

        try
        {
            var job = await GenerateJobFromTemplateAsync(template, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (job != null)
            {
                return new GenerateJobsResult(1, new List<Guid> { job.Id }, new List<string>());
            }
            return new GenerateJobsResult(0, new List<Guid>(), new List<string> { "Job could not be generated" });
        }
        catch (Exception ex)
        {
            return new GenerateJobsResult(0, new List<Guid>(), new List<string> { ex.Message });
        }
    }

    public async Task<RecurringJobHistoryResponse> GetJobHistoryAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await _context.RecurringJobTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException("Template not found");
        }

        var jobs = await _context.Jobs
            .Where(j => j.RecurringTemplateId == templateId)
            .OrderByDescending(j => j.RecurrenceNumber)
            .ToListAsync(cancellationToken);

        return new RecurringJobHistoryResponse(
            TemplateId: templateId,
            TemplateTitle: template.Title,
            Jobs: jobs.Select(j => new RecurringJobHistoryDto(
                JobId: j.Id,
                Title: j.Title,
                RecurrenceNumber: j.RecurrenceNumber ?? 0,
                Status: j.Status.ToString(),
                CreatedAt: j.CreatedAt,
                CompletedAt: j.CompletedAt
            )).ToList(),
            TotalCount: jobs.Count
        );
    }

    public DateTime CalculateNextDueDate(DateTime fromDate, RecurrencePattern pattern, int customIntervalDays = 0)
    {
        return pattern switch
        {
            RecurrencePattern.Weekly => fromDate.AddDays(7),
            RecurrencePattern.Fortnightly => fromDate.AddDays(14),
            RecurrencePattern.Monthly => fromDate.AddMonths(1),
            RecurrencePattern.Quarterly => fromDate.AddMonths(3),
            RecurrencePattern.SemiAnnually => fromDate.AddMonths(6),
            RecurrencePattern.Annually => fromDate.AddYears(1),
            _ => customIntervalDays > 0 ? fromDate.AddDays(customIntervalDays) : fromDate.AddMonths(1)
        };
    }

    private async Task<Job?> GenerateJobFromTemplateAsync(RecurringJobTemplate template, CancellationToken cancellationToken)
    {
        // Check if max occurrences reached
        if (template.MaxOccurrences.HasValue && template.OccurrencesGenerated >= template.MaxOccurrences.Value)
        {
            template.IsActive = false;
            return null;
        }

        // Check if end date passed
        if (template.EndDate.HasValue && template.EndDate.Value < DateTime.UtcNow)
        {
            template.IsActive = false;
            return null;
        }

        // Parse state
        if (!Enum.TryParse<AustralianState>(template.State, true, out var state))
        {
            state = AustralianState.NSW; // Default
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = template.CustomerId,
            TradeCategoryId = template.TradeCategoryId,
            Title = $"{template.Title} #{template.OccurrencesGenerated + 1}",
            Description = template.Description ?? "",
            Status = JobStatus.Open,
            BudgetMin = template.EstimatedBudgetMin,
            BudgetMax = template.EstimatedBudgetMax,
            PreferredStartDate = template.NextDueDate,
            IsFlexibleDates = true,
            Latitude = template.Latitude ?? 0,
            Longitude = template.Longitude ?? 0,
            SuburbName = template.SuburbName,
            State = state,
            Postcode = template.PostCode ?? "",
            PropertyType = PropertyType.House, // Default
            RecurringTemplateId = template.Id,
            RecurrenceNumber = template.OccurrencesGenerated + 1,
            PublicVisibleFrom = DateTime.UtcNow
        };

        _context.Jobs.Add(job);

        // Update template
        template.OccurrencesGenerated++;
        template.LastGeneratedAt = DateTime.UtcNow;
        template.NextDueDate = CalculateNextDueDate(template.NextDueDate ?? DateTime.UtcNow, template.Pattern, template.CustomIntervalDays);

        // Check if max occurrences now reached
        if (template.MaxOccurrences.HasValue && template.OccurrencesGenerated >= template.MaxOccurrences.Value)
        {
            template.IsActive = false;
        }

        // Check if end date will be passed before next occurrence
        if (template.EndDate.HasValue && template.NextDueDate > template.EndDate.Value)
        {
            template.IsActive = false;
        }

        return job;
    }

    private RecurringJobTemplateDto MapToDto(RecurringJobTemplate template)
    {
        return new RecurringJobTemplateDto(
            Id: template.Id,
            CustomerId: template.CustomerId,
            CustomerName: $"{template.Customer.FirstName} {template.Customer.LastName}",
            TradieId: template.TradieId,
            TradieName: template.Tradie != null ? $"{template.Tradie.FirstName} {template.Tradie.LastName}" : null,
            TradeCategoryId: template.TradeCategoryId,
            TradeCategoryName: template.TradeCategory.Name,
            Title: template.Title,
            Description: template.Description,
            Address: template.Address,
            SuburbName: template.SuburbName,
            PostCode: template.PostCode,
            State: template.State,
            Latitude: template.Latitude,
            Longitude: template.Longitude,
            Pattern: template.Pattern.ToString(),
            CustomIntervalDays: template.CustomIntervalDays,
            StartDate: template.StartDate,
            EndDate: template.EndDate,
            MaxOccurrences: template.MaxOccurrences,
            OccurrencesGenerated: template.OccurrencesGenerated,
            EstimatedBudgetMin: template.EstimatedBudgetMin,
            EstimatedBudgetMax: template.EstimatedBudgetMax,
            IsActive: template.IsActive,
            LastGeneratedAt: template.LastGeneratedAt,
            NextDueDate: template.NextDueDate,
            Notes: template.Notes,
            AutoAcceptFromTradie: template.AutoAcceptFromTradie,
            CreatedAt: template.CreatedAt
        );
    }
}
