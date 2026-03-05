using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelemetrySlice.Data;

namespace TelemetrySlice.Core.Extensions;

public static class AppExtensions
{
    public static void ConfigureAndRun(this WebApplication app, bool migrate = false)
    {
        if (migrate)
        {
            using (var serviceScope = app.Services.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<TelemetrySliceDbContext>();
                var migrator = dbContext.Database.GetInfrastructure().GetService<IMigrator>()!;
                migrator.Migrate();
            
                // Enable WAL and optimize synchronous mode
                dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                dbContext.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
            }
        }
        
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        
        app.Run();
    }
}