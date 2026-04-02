using Microsoft.EntityFrameworkCore;
using StressTestApp.Server.Core.Database.Entities;

namespace StressTestApp.Server.Core.Database
{
    public interface IStressTestDbContext
    {
        public DbSet<Calculation> Calculations { get; }
        public DbSet<CalculationInput> CalculationInputs { get; }
        public DbSet<CalculationResult> CalculationResults { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
