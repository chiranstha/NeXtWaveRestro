using NextWave.Erp.Restaurant;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.Restaurant;

public class RestaurantPinHasher_Tests
{
    [Fact]
    public void Hashed_pin_verifies_without_exposing_plaintext()
    {
        var stored = RestaurantPinHasher.Hash("2741");

        stored.ShouldNotContain("2741");
        RestaurantPinHasher.Verify(stored, "2741", out var wasLegacy).ShouldBeTrue();
        wasLegacy.ShouldBeFalse();
        RestaurantPinHasher.Verify(stored, "2742", out _).ShouldBeFalse();
    }

    [Fact]
    public void Legacy_pin_can_be_verified_for_one_time_migration()
    {
        RestaurantPinHasher.Verify("2741", "2741", out var wasLegacy).ShouldBeTrue();
        wasLegacy.ShouldBeTrue();
        RestaurantPinHasher.Verify("2741", "nope", out _).ShouldBeFalse();
    }
}
