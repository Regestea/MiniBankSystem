using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>Design-time factory for EF migrations.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MiniBankDbContext>
{
    public MiniBankDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__minibankdb")
            ?? "Host=localhost;Port=5432;Database=minibankdb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MiniBankDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new MiniBankDbContext(options);
    }
}
