using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OppSignal.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations add` at design time without booting the app.
/// The runtime connection string comes from configuration/env in Program.cs;
/// this only needs a valid provider + a placeholder connection for scaffolding.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                   ?? "Host=localhost;Port=5432;Database=oppsignal;Username=oppsignal;Password=oppsignal";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conn, o => o.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
