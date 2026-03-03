using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Data.Cotahist;
using Infrastructure.Data.Context;
using Infrastructure.Data.EventStore;
using Infrastructure.Data.Kafka;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<DataContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ITopFiveBasketRepository, TopFiveBasketRepository>();
        services.AddScoped<IMasterCustodyRepository, MasterCustodyRepository>();
        services.AddScoped<ICustomerCustodyRepository, CustomerCustodyRepository>();
        services.AddScoped<IBuyCycleRepository, BuyCycleRepository>();
        services.AddScoped<IAssetPriceRepository, AssetPriceRepository>();

        // Event Store
        services.AddScoped<EventSerializer>();
        services.AddScoped<IEventStore, SqlEventStore>();

        // Kafka
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        services.AddSingleton<IKafkaProducer>(new KafkaProducer(kafkaBootstrap));

        // COTAHIST
        services.AddSingleton<CotahistParser>();
        services.AddScoped<CotahistImportService>();

        // Application services
        services.AddScoped<IrCalculationService>();

        var irDedoDuroTopic    = configuration["Kafka:TopicIrDedoDuro"]    ?? "ir-dedo-duro";
        var irRebalancingTopic = configuration["Kafka:TopicIrRebalancing"] ?? "ir-rebalancing";
        var deviationThreshold = decimal.TryParse(
            configuration["Rebalancing:DeviationThresholdPercent"],
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal parsed) ? parsed : 5m;

        services.AddScoped<IPurchaseEngineService>(sp => new PurchaseEngineService(
            sp.GetRequiredService<ICustomerRepository>(),
            sp.GetRequiredService<ITopFiveBasketRepository>(),
            sp.GetRequiredService<IAssetPriceRepository>(),
            sp.GetRequiredService<IMasterCustodyRepository>(),
            sp.GetRequiredService<ICustomerCustodyRepository>(),
            sp.GetRequiredService<IBuyCycleRepository>(),
            sp.GetRequiredService<IEventStore>(),
            sp.GetRequiredService<IKafkaProducer>(),
            sp.GetRequiredService<IrCalculationService>(),
            irDedoDuroTopic));

        services.AddScoped<IRebalancingEngineService>(sp => new RebalancingEngineService(
            sp.GetRequiredService<ICustomerRepository>(),
            sp.GetRequiredService<IAssetPriceRepository>(),
            sp.GetRequiredService<ICustomerCustodyRepository>(),
            sp.GetRequiredService<IEventStore>(),
            sp.GetRequiredService<IKafkaProducer>(),
            sp.GetRequiredService<IrCalculationService>(),
            deviationThreshold,
            irRebalancingTopic));

        return services;
    }
}
