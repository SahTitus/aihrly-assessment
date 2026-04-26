using Aihrly.Api.Entities;
using Aihrly.Api.Services;

namespace Aihrly.Tests;

public class StageTransitionTests
{
    [Theory]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Screening)]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Screening, ApplicationStage.Interview)]
    [InlineData(ApplicationStage.Screening, ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Interview, ApplicationStage.Offer)]
    [InlineData(ApplicationStage.Interview, ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Offer,     ApplicationStage.Hired)]
    [InlineData(ApplicationStage.Offer,     ApplicationStage.Rejected)]
    public void Valid_transitions_are_allowed(ApplicationStage from, ApplicationStage to)
    {
        Assert.True(StageTransitionValidator.IsValid(from, to));
    }

    [Theory]
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Interview)]  // skip a stage
    [InlineData(ApplicationStage.Applied,   ApplicationStage.Hired)]      // way too far
    [InlineData(ApplicationStage.Screening, ApplicationStage.Offer)]      // skip interview
    [InlineData(ApplicationStage.Hired,     ApplicationStage.Rejected)]   // terminal
    [InlineData(ApplicationStage.Rejected,  ApplicationStage.Screening)]  // can't reopen
    public void Invalid_transitions_are_rejected(ApplicationStage from, ApplicationStage to)
    {
        Assert.False(StageTransitionValidator.IsValid(from, to));
    }
}
