using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Domain.Entities;

namespace StressTestApp.Server.Data;

public class StressTestDbContext : DbContext
{
    public StressTestDbContext(DbContextOptions<StressTestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Calculation> Calculations => Set<Calculation>();
    public DbSet<CalculationInput> CalculationInputs => Set<CalculationInput>();
    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Calculation entity
        modelBuilder.Entity<Calculation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.PortfolioCount).IsRequired();
            entity.Property(e => e.LoanCount).IsRequired();
            entity.Property(e => e.TotalExpectedLoss).HasPrecision(18, 2);

            // Configure navigation properties
            entity.HasMany(e => e.Inputs)
                .WithOne(i => i.Calculation)
                .HasForeignKey(i => i.CalculationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Results)
                .WithOne(r => r.Calculation)
                .HasForeignKey(r => r.CalculationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CalculationInput entity
        modelBuilder.Entity<CalculationInput>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CalculationId).IsRequired();
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.HousePriceChange).HasPrecision(18, 4);
            
            entity.HasIndex(e => e.CalculationId);
        });

        // Configure CalculationResult entity
        modelBuilder.Entity<CalculationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CalculationId).IsRequired();
            entity.Property(e => e.PortfolioId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PortfolioName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TotalOutstandingAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalCollateralValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalScenarioCollateralValue).HasPrecision(18, 2);
            entity.Property(e => e.TotalExpectedLoss).HasPrecision(18, 2);
            entity.Property(e => e.LoanCount).IsRequired();
            
            entity.HasIndex(e => e.CalculationId);
        });
    }
}
