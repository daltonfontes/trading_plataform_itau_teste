using Domain.Entities;
using Infrastructure.Data.EventStore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Context;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TopFiveBasket> TopFiveBaskets => Set<TopFiveBasket>();
    public DbSet<BasketComposition> BasketCompositions => Set<BasketComposition>();
    public DbSet<MasterCustodyItem> MasterCustodyItems => Set<MasterCustodyItem>();
    public DbSet<CustomerCustodyItem> CustomerCustodyItems => Set<CustomerCustodyItem>();
    public DbSet<BuyCycle> BuyCycles => Set<BuyCycle>();
    public DbSet<DistributionRecord> DistributionRecords => Set<DistributionRecord>();
    public DbSet<AssetPrice> AssetPrices => Set<AssetPrice>();
    public DbSet<EventStoreEntry> EventStoreEntries => Set<EventStoreEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Asset
        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Ticker).IsUnique();
            e.Property(a => a.Ticker).HasMaxLength(12).IsRequired();
            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.CPF).IsUnique();
            e.Property(c => c.CPF).HasMaxLength(14).IsRequired();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.Property(c => c.MonthlyContribution).HasPrecision(18, 2);
            e.Property(c => c.Status).HasConversion<string>();
        });

        // TopFiveBasket
        modelBuilder.Entity<TopFiveBasket>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Status).HasConversion<string>();
            e.HasMany(b => b.Compositions)
             .WithOne()
             .HasForeignKey(c => c.TopFiveBasketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // BasketComposition
        modelBuilder.Entity<BasketComposition>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Percentage).HasPrecision(5, 2);
            e.HasOne(c => c.Asset).WithMany().HasForeignKey(c => c.AssetId);
        });

        // MasterCustodyItem
        modelBuilder.Entity<MasterCustodyItem>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.AssetId).IsUnique();
            e.Property(m => m.Quantity).HasPrecision(18, 6);
            e.Property(m => m.AveragePrice).HasPrecision(18, 6);
            e.HasOne(m => m.Asset).WithMany().HasForeignKey(m => m.AssetId);
        });

        // CustomerCustodyItem
        modelBuilder.Entity<CustomerCustodyItem>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.CustomerId, c.AssetId }).IsUnique();
            e.Property(c => c.Quantity).HasPrecision(18, 6);
            e.Property(c => c.AveragePrice).HasPrecision(18, 6);
            e.HasOne(c => c.Asset).WithMany().HasForeignKey(c => c.AssetId);
        });

        // BuyCycle
        modelBuilder.Entity<BuyCycle>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.Date, b.Installment });
            e.Property(b => b.Status).HasConversion<string>();
            e.Property(b => b.Installment).HasConversion<string>();
            e.Property(b => b.TotalValue).HasPrecision(18, 2);
        });

        // DistributionRecord
        modelBuilder.Entity<DistributionRecord>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Quantity).HasPrecision(18, 6);
            e.Property(d => d.UnitPrice).HasPrecision(18, 6);
            e.Property(d => d.TotalValue).HasPrecision(18, 2);
            e.Property(d => d.IrTax).HasPrecision(18, 6);
            e.HasOne(d => d.Asset).WithMany().HasForeignKey(d => d.AssetId);
            e.HasOne(d => d.Customer).WithMany().HasForeignKey(d => d.CustomerId);
        });

        // AssetPrice
        modelBuilder.Entity<AssetPrice>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.AssetId, p.TradingDate }).IsUnique();
            e.Property(p => p.ClosingPrice).HasPrecision(18, 6);
            e.HasOne(p => p.Asset).WithMany().HasForeignKey(p => p.AssetId);
        });

        // EventStoreEntry
        modelBuilder.Entity<EventStoreEntry>(e =>
        {
            e.HasKey(es => es.Sequence);
            e.Property(es => es.Sequence).ValueGeneratedOnAdd();
            e.HasIndex(es => es.EventId).IsUnique();
            e.HasIndex(es => es.AggregateId);
            e.Property(es => es.EventType).HasMaxLength(100).IsRequired();
            e.Property(es => es.Payload).HasColumnType("longtext").IsRequired();
        });
    }
}
