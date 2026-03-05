using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace TelemetrySlice.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TelemetrySliceDbContext>
{
    public TelemetrySliceDbContext CreateDbContext(string[] args)
    {

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json")
            .Build();
            
        var builder = new DbContextOptionsBuilder<TelemetrySliceDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
            
        builder.UseSqlite(connectionString);
            
        return new TelemetrySliceDbContext(builder.Options);
    }
}