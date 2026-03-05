namespace TelemetrySlice.Domain.Interfaces;

public interface IAdminService
{
    Task SeedAsync(int intervalSeconds = 30);
}