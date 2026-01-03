using Quote.Domain.Entities;
using Quote.Shared.DTOs;

namespace Quote.Application.RecurringJobs.Services;

public interface IRecurringJobService
{
    Task<RecurringJobTemplate> CreateTemplateAsync(Guid customerId, CreateRecurringJobTemplateRequest request, CancellationToken cancellationToken = default);
    Task<RecurringJobTemplate?> UpdateTemplateAsync(Guid customerId, Guid templateId, UpdateRecurringJobTemplateRequest request, CancellationToken cancellationToken = default);
    Task<RecurringJobTemplateDto?> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<RecurringJobTemplatesResponse> GetTemplatesAsync(Guid userId, bool isTradie, int page, int pageSize, bool? activeOnly, CancellationToken cancellationToken = default);
    Task<bool> DeactivateTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> ActivateTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken = default);
    Task<GenerateJobsResult> GenerateDueJobsAsync(CancellationToken cancellationToken = default);
    Task<GenerateJobsResult> GenerateJobsForTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<RecurringJobHistoryResponse> GetJobHistoryAsync(Guid templateId, CancellationToken cancellationToken = default);
    DateTime CalculateNextDueDate(DateTime fromDate, Domain.Enums.RecurrencePattern pattern, int customIntervalDays = 0);
}
