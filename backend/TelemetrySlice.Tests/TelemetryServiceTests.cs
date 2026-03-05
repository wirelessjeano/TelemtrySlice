using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.Models;
using TelemetrySlice.Services;
using Xunit;

namespace TelemetrySlice.Tests;

public class TelemetryServiceTests : IDisposable
{
    private readonly TelemetrySliceDbContext _db;
    private readonly TelemetryService _service;

    public TelemetryServiceTests()
    {
        var options = new DbContextOptionsBuilder<TelemetrySliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TelemetrySliceDbContext(options);
        _service = new TelemetryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private void SeedEvents(string customerId, string deviceId, params (DateTime recordedAt, double value)[] entries)
    {
        foreach (var (recordedAt, value) in entries)
        {
            _db.TelemetryEvents.Add(new TelemetryEvent
            {
                CustomerId = customerId,
                DeviceId = deviceId,
                EventId = Guid.NewGuid().ToString(),
                RecordedAt = recordedAt,
                Value = value
            });
        }
        _db.SaveChanges();
    }

    // --- GetChartDataAsync ---

    [Fact]
    public async Task GetChartDataAsync_ReturnsEventsWithinLast24Hours()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-23), 10.0),
            (now.AddHours(-12), 20.0),
            (now.AddHours(-1), 30.0));

        var result = await _service.GetChartDataAsync("c1", "d1");

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetChartDataAsync_ExcludesEventsOlderThan24Hours()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-25), 10.0),
            (now.AddHours(-1), 20.0));

        var result = await _service.GetChartDataAsync("c1", "d1");

        Assert.Single(result);
        Assert.Equal(20.0, result[0].Value);
    }

    [Fact]
    public async Task GetChartDataAsync_OrdersByRecordedAtAscending()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-3), 30.0),
            (now.AddHours(-10), 10.0),
            (now.AddHours(-6), 20.0));

        var result = await _service.GetChartDataAsync("c1", "d1");

        Assert.Equal(10.0, result[0].Value);
        Assert.Equal(20.0, result[1].Value);
        Assert.Equal(30.0, result[2].Value);
    }

    [Fact]
    public async Task GetChartDataAsync_FiltersbyCustomerAndDevice()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1", (now.AddHours(-1), 10.0));
        SeedEvents("c1", "d2", (now.AddHours(-1), 20.0));
        SeedEvents("c2", "d1", (now.AddHours(-1), 30.0));

        var result = await _service.GetChartDataAsync("c1", "d1");

        Assert.Single(result);
        Assert.Equal(10.0, result[0].Value);
    }

    [Fact]
    public async Task GetChartDataAsync_ReturnsEmptyWhenNoData()
    {
        var result = await _service.GetChartDataAsync("c1", "d1");

        Assert.Empty(result);
    }

    // --- GetPagedDataAsync ---

    [Fact]
    public async Task GetPagedDataAsync_ReturnsCorrectPage()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 10; i++)
            SeedEvents("c1", "d1", (now.AddMinutes(-i), i));

        var result = await _service.GetPagedDataAsync("c1", "d1", page: 1, pageSize: 3);

        Assert.Equal(3, result.Data.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetPagedDataAsync_OrdersByRecordedAtDescending()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-10), 10.0),
            (now.AddHours(-5), 20.0),
            (now.AddHours(-1), 30.0));

        var result = await _service.GetPagedDataAsync("c1", "d1", page: 1, pageSize: 10);

        Assert.Equal(30.0, result.Data[0].Value);
        Assert.Equal(20.0, result.Data[1].Value);
        Assert.Equal(10.0, result.Data[2].Value);
    }

    [Fact]
    public async Task GetPagedDataAsync_SecondPageReturnsRemainingItems()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
            SeedEvents("c1", "d1", (now.AddMinutes(-i), i));

        var result = await _service.GetPagedDataAsync("c1", "d1", page: 2, pageSize: 3);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedDataAsync_ExcludesOldEvents()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-25), 10.0),
            (now.AddHours(-1), 20.0));

        var result = await _service.GetPagedDataAsync("c1", "d1", page: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetPagedDataAsync_ReturnsEmptyWhenNoData()
    {
        var result = await _service.GetPagedDataAsync("c1", "d1", page: 1, pageSize: 10);

        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    // --- GetMetricsAsync ---

    [Fact]
    public async Task GetMetricsAsync_ReturnsCorrectAggregates()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-3), 10.0),
            (now.AddHours(-2), 20.0),
            (now.AddHours(-1), 30.0));

        var result = await _service.GetMetricsAsync("c1", "d1");

        Assert.NotNull(result);
        Assert.Equal(30.0, result.Latest);
        Assert.Equal(10.0, result.Minimum);
        Assert.Equal(30.0, result.Maximum);
        Assert.Equal(20.0, result.Average);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsNullWhenNoData()
    {
        var result = await _service.GetMetricsAsync("c1", "d1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMetricsAsync_ExcludesOldEvents()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1",
            (now.AddHours(-25), 100.0),
            (now.AddHours(-1), 50.0));

        var result = await _service.GetMetricsAsync("c1", "d1");

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(50.0, result.Latest);
        Assert.Equal(50.0, result.Minimum);
        Assert.Equal(50.0, result.Maximum);
    }

    [Fact]
    public async Task GetMetricsAsync_FiltersbyCustomerAndDevice()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1", (now.AddHours(-1), 10.0));
        SeedEvents("c1", "d2", (now.AddHours(-1), 99.0));

        var result = await _service.GetMetricsAsync("c1", "d1");

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(10.0, result.Latest);
    }

    [Fact]
    public async Task GetMetricsAsync_SingleEvent_AllMetricsEqual()
    {
        var now = DateTime.UtcNow;
        SeedEvents("c1", "d1", (now.AddHours(-1), 42.0));

        var result = await _service.GetMetricsAsync("c1", "d1");

        Assert.NotNull(result);
        Assert.Equal(42.0, result.Latest);
        Assert.Equal(42.0, result.Minimum);
        Assert.Equal(42.0, result.Maximum);
        Assert.Equal(42.0, result.Average);
        Assert.Equal(1, result.TotalCount);
    }
}