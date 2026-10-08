using Idp.Swiyu.Passkeys.Sts.Domain.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddApplicationDbContext("database");

builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
await host.RunAsync();
