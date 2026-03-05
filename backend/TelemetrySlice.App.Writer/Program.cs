using TelemetrySlice.App.Writer.Queues;
using TelemetrySlice.App.Writer.Services;
using TelemetrySlice.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<IncomingTelemetryService>(); 
builder.Services.AddSingleton<DatabaseWriterQueue>();
builder.Services.AddHostedService<DatabaseWriterService>();

var app = builder.ConfigureAndBuild( "Writer");

app.ConfigureAndRun();