using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.LeadScoring.Queries.GetJobScore;

public record GetJobScoreQuery(Guid TradieId, Guid JobId) : IRequest<Result<LeadScoreDetailDto>>;
