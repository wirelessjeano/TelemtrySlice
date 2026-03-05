using TelemetrySlice.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder.ConfigureAndBuild( "API");

app.ConfigureAndRun(migrate:true);