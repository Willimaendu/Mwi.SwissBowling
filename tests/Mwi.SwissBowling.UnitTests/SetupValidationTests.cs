using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.UnitTests;

public sealed class SetupValidationTests
{
    [Fact]
    public void GuestDoesNotRequireSfkvNumberOrBirthDate() => Assert.Null(SetupValidation.Player(new PlayerInput("Beispiel", "Gast", null, null, null, null), new DateOnly(2026, 1, 1)));
    [Theory]
    [InlineData("abc")]
    [InlineData("123-456")]
    public void RejectsInvalidSfkvNumber(string number) => Assert.NotNull(SetupValidation.Player(new PlayerInput("A", "B", number, null, null, null), new DateOnly(2026, 1, 1)));
    [Fact]
    public void RejectsFutureBirthDate() => Assert.NotNull(SetupValidation.Player(new PlayerInput("A", "B", "123", new DateOnly(2027, 1, 1), null, null), new DateOnly(2026, 1, 1)));
    [Fact]
    public void ThrowLimitIsOptional() => Assert.Null(SetupValidation.Event(new EventInput(Guid.NewGuid(), "Anlass", Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), null)));
}
