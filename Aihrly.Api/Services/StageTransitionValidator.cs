using Aihrly.Api.Entities;

namespace Aihrly.Api.Services;

public static class StageTransitionValidator
{
    private static readonly Dictionary<ApplicationStage, ApplicationStage[]> Allowed = new()
    {
        [ApplicationStage.Applied]   = [ApplicationStage.Screening, ApplicationStage.Rejected],
        [ApplicationStage.Screening] = [ApplicationStage.Interview, ApplicationStage.Rejected],
        [ApplicationStage.Interview] = [ApplicationStage.Offer, ApplicationStage.Rejected],
        [ApplicationStage.Offer]     = [ApplicationStage.Hired, ApplicationStage.Rejected],
        // terminal states
        [ApplicationStage.Hired]     = [],
        [ApplicationStage.Rejected]  = [],
    };

    public static bool IsValid(ApplicationStage from, ApplicationStage to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static string[] ValidNext(ApplicationStage from)
        => Allowed.TryGetValue(from, out var next) ? next.Select(s => s.ToString()).ToArray() : [];
}
