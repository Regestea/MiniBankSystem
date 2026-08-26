using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniBank.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` at design time (no Aspire host running).
/// Connection string is a local placeholder — never used at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MiniBankDbContext>
{
    public MiniBankDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MiniBankDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=minibankdb;Username=postgres;Password=postgres")
            .Options;

        return new MiniBankDbContext(options);
    }
}
