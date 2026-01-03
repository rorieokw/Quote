namespace Quote.Shared.DTOs;

public record TeamMemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    string Role,
    decimal? HourlyRate,
    bool IsActive,
    int TotalAssignments,
    DateTime CreatedAt
);

public record CreateTeamMemberRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role,
    decimal? HourlyRate
);

public record UpdateTeamMemberRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role,
    decimal? HourlyRate,
    bool IsActive
);

public record JobAssignmentDto(
    Guid Id,
    Guid JobQuoteId,
    string JobTitle,
    Guid TeamMemberId,
    string TeamMemberName,
    DateTime ScheduledDate,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Notes
);

public record CreateJobAssignmentRequest(
    Guid JobQuoteId,
    Guid TeamMemberId,
    DateTime ScheduledDate,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Notes
);

public record TeamScheduleDto(
    DateTime Date,
    List<JobAssignmentDto> Assignments
);

public record TeamListResponse(
    List<TeamMemberDto> Members,
    int TotalCount
);
