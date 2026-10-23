using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.UnitTests;

public sealed class ScoreRulesTests
{
    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 0, -1)]
    [InlineData(8, 1, 0)]
    [InlineData(900, 0, 0)]
    [InlineData(900, 101, 0)]
    public void RejectsContradictoryAggregates(int total, int nines, int eights) => Assert.NotNull(ScoreRules.Validate(new ResultInput(total, nines, eights, false, 0, Guid.NewGuid()), 100));
    [Fact]
    public void AcceptsValidAggregate() => Assert.Null(ScoreRules.Validate(new ResultInput(782, 30, 24, false, 0, Guid.NewGuid()), 100));
    [Fact]
    public void PreservesPartialInterruptedScore() => Assert.Null(ScoreRules.Validate(new ResultInput(80, 0, 10, true, 0, Guid.NewGuid()), 100));
    [Fact]
    public void DoesNotAssumeThrowLimit() => Assert.Null(ScoreRules.Validate(new ResultInput(1800, 200, 0, false, 0, Guid.NewGuid()), null));
    [Fact]
    public void HandlesExtremeInputWithoutIntegerOverflow() => Assert.NotNull(ScoreRules.Validate(new ResultInput(int.MaxValue, int.MaxValue, int.MaxValue, false, 0, Guid.NewGuid()), int.MaxValue));
}
