using MediatR;
using Quote.Application.Common.Models;
using Quote.Application.LeadScoring.Services;

namespace Quote.Application.LeadScoring.Commands.RecalculateScores;

public class RecalculateScoresCommandHandler : IRequestHandler<RecalculateScoresCommand, Result>
{
    private readonly ILeadScoringService _scoringService;

    public RecalculateScoresCommandHandler(ILeadScoringService scoringService)
    {
        _scoringService = scoringService;
    }

    public async Task<Result> Handle(RecalculateScoresCommand request, CancellationToken cancellationToken)
    {
        await _scoringService.RecalculateAllScoresForTradieAsync(request.TradieId, cancellationToken);
        return Result.Success();
    }
}
