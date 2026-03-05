using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Data;

public class TelemetrySliceDbContext(DbContextOptions<TelemetrySliceDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => new  { e.CustomerId, e.DeviceId });
            entity.HasIndex(e => e.CustomerId);
        });

        modelBuilder.Entity<TelemetryEvent>(entity =>
        {
            entity.HasKey(e => new { e.CustomerId, e.DeviceId, e.EventId });
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => new { e.CustomerId, e.DeviceId, e.RecordedAt });
        });
    }
}