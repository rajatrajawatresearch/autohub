using AutoHub.SharedKernel.Money;

namespace AutoHub.Tests.Unit.SharedKernel;

public class MoneyTests
{
    [Fact]
    public void FromRupees_converts_to_paise()
    {
        Money.FromRupees(847_350m).MinorUnits.Should().Be(84_735_000);
    }

    [Fact]
    public void FromRupees_rejects_amounts_it_cannot_represent_exactly()
    {
        // Sub-paise precision is where reconciliation breaks. Better a loud throw at the boundary
        // than a silent half-paisa that only surfaces at month-end close.
        var act = () => Money.FromRupees(100.005m);

        act.Should().Throw<ArgumentException>().WithMessage("*cannot be represented exactly*");
    }

    [Fact]
    public void Arithmetic_across_currencies_is_refused()
    {
        var rupees = Money.FromRupees(1000);
        var dollars = Money.FromMajorUnits(1000, Currency.Usd);

        var act = () => _ = rupees + dollars;

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(84_735_000, 0.02, 1_694_700)] // 2% auction commission on ₹8,47,350
    [InlineData(1_694_700, 0.18, 305_046)]    // 18% GST on that commission
    public void MultiplyByRate_computes_exact_minor_units(long baseMinor, double rate, long expected)
    {
        var result = Money.FromMinorUnits(baseMinor, Currency.Inr).MultiplyByRate((decimal)rate);

        result.MinorUnits.Should().Be(expected);
    }

    [Fact]
    public void MultiplyByRate_uses_bankers_rounding_by_default()
    {
        // 0.5 paise rounds to even, not always up. Always-up quietly transfers real money to the
        // platform across a million transactions, which is the kind of thing auditors find.
        Money.FromMinorUnits(5, Currency.Inr).MultiplyByRate(0.5m).MinorUnits.Should().Be(2);
        Money.FromMinorUnits(7, Currency.Inr).MultiplyByRate(0.5m).MinorUnits.Should().Be(4);
    }

    [Fact]
    public void Allocate_distributes_the_remainder_so_parts_sum_to_the_whole()
    {
        // ₹100.00 three ways is the canonical example: naive division gives ₹99.99.
        var parts = Money.FromRupees(100).Allocate([1, 1, 1]);

        parts.Select(p => p.MinorUnits).Should().Equal(3334, 3333, 3333);
        parts.Aggregate(Money.ZeroInr, (a, b) => a + b).Should().Be(Money.FromRupees(100));
    }

    [Fact]
    public void Allocate_respects_uneven_ratios_and_still_balances()
    {
        var total = Money.FromMinorUnits(84_735_000, Currency.Inr);

        var parts = total.Allocate([70, 20, 10]);

        parts.Aggregate(Money.ZeroInr, (a, b) => a + b).Should().Be(total);
        parts[0].MinorUnits.Should().BeGreaterThan(parts[1].MinorUnits);
    }

    [Fact]
    public void Allocate_balances_negative_amounts_too()
    {
        // Refunds and credit notes travel as negative money and must still reconcile.
        var refund = Money.FromRupees(-100);

        var parts = refund.Allocate([1, 1, 1]);

        parts.Aggregate(Money.ZeroInr, (a, b) => a + b).Should().Be(refund);
    }

    [Fact]
    public void Clamp_applies_floor_and_cap()
    {
        var fee = Money.FromRupees(50);

        fee.Clamp(Money.FromRupees(199), null).Should().Be(Money.FromRupees(199));
        Money.FromRupees(90_000).Clamp(null, Money.FromRupees(25_000))
            .Should().Be(Money.FromRupees(25_000));
    }
}
