using Domain.Aggregates;
using Domain.Events;
using Xunit;

namespace UnitTests.Aggregates;

public class CustomerCustodyAggregateTests
{
    // --- Preço Médio ---

    [Fact]
    public void RegisterContribuition_FirstPurchase_SetsPriceCorrectly()
    {
        var aggregate = new CustomerCustodyAggregate();

        aggregate.RegisterContribuition("PETR4", 8, 35m, 0.014m, Guid.NewGuid());

        Assert.Equal(8m, aggregate.Positions["PETR4"]);
        Assert.Equal(35m, aggregate.AveragePrice["PETR4"]);
    }

    [Fact]
    public void RegisterContribuition_SecondPurchase_UpdatesWeightedAverage()
    {
        var aggregate = new CustomerCustodyAggregate();

        aggregate.RegisterContribuition("PETR4", 8, 35m, 0m, Guid.NewGuid());
        aggregate.RegisterContribuition("PETR4", 10, 37m, 0m, Guid.NewGuid());

        // PM = (8*35 + 10*37) / 18 = (280+370)/18 = 36.11...
        decimal expectedPm = (8 * 35m + 10 * 37m) / 18m;
        Assert.Equal(18m, aggregate.Positions["PETR4"]);
        Assert.Equal(expectedPm, aggregate.AveragePrice["PETR4"]);
    }

    [Fact]
    public void RegisterSale_DoesNotChangeAveragePrice()
    {
        var aggregate = new CustomerCustodyAggregate();
        aggregate.RegisterContribuition("PETR4", 8, 35m, 0m, Guid.NewGuid());
        decimal pmAntes = aggregate.AveragePrice["PETR4"];

        aggregate.RegisterSale("PETR4", 3, 40m, 15m);

        Assert.Equal(5m, aggregate.Positions["PETR4"]);
        Assert.Equal(pmAntes, aggregate.AveragePrice["PETR4"]); // PM não muda em venda
    }

    [Fact]
    public void RegisterSale_ReducesPosition()
    {
        var aggregate = new CustomerCustodyAggregate();
        aggregate.RegisterContribuition("PETR4", 18, 36.11m, 0m, Guid.NewGuid());

        aggregate.RegisterSale("PETR4", 5, 40m, 19.45m);

        Assert.Equal(13m, aggregate.Positions["PETR4"]);
    }

    // --- Recreate via eventos ---

    [Fact]
    public void Recreate_FromEvents_RebuildsStateCorrectly()
    {
        var customerId = Guid.NewGuid();
        var cicleId = Guid.NewGuid();

        var events = new Domain.Events.Base.DomainEvent[]
        {
            new DistributedContribution(Guid.NewGuid(), DateTime.UtcNow, customerId, "PETR4", 8, 35m, 280m, 0.014m, cicleId),
            new DistributedContribution(Guid.NewGuid(), DateTime.UtcNow, customerId, "PETR4", 10, 37m, 370m, 0.018m, cicleId),
            new AssetsSoldRebalancing(Guid.NewGuid(), DateTime.UtcNow, customerId, "PETR4", 5, 40m, 19.45m)
        };

        var aggregate = CustomerCustodyAggregate.Recreate(events);

        Assert.Equal(13m, aggregate.Positions["PETR4"]);
        decimal expectedPm = (8 * 35m + 10 * 37m) / 18m;
        Assert.Equal(expectedPm, aggregate.AveragePrice["PETR4"]);
    }

    [Fact]
    public void GetEvents_ReturnsOnlyNewEvents()
    {
        var aggregate = new CustomerCustodyAggregate();
        aggregate.RegisterContribuition("VALE3", 4, 62m, 0m, Guid.NewGuid());
        aggregate.RegisterContribuition("VALE3", 8, 65m, 0m, Guid.NewGuid());

        var events = aggregate.GetEvents();

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void MultipleAssets_TrackedIndependently()
    {
        var aggregate = new CustomerCustodyAggregate();
        aggregate.RegisterContribuition("PETR4", 10, 35m, 0m, Guid.NewGuid());
        aggregate.RegisterContribuition("VALE3", 5, 62m, 0m, Guid.NewGuid());

        Assert.Equal(10m, aggregate.Positions["PETR4"]);
        Assert.Equal(5m, aggregate.Positions["VALE3"]);
        Assert.Equal(35m, aggregate.AveragePrice["PETR4"]);
        Assert.Equal(62m, aggregate.AveragePrice["VALE3"]);
    }
}
