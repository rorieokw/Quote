using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.LeadScoring.Commands.RecalculateScores;

public record RecalculateScoresCommand(Guid TradieId) : IRequest<Result>;
