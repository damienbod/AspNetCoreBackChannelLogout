using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;

namespace MvcHybrid
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
           .Enrich.FromLogContext()
           .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                BuildWebHost(args).Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                var builder = config.Build();
                var keyVaultEndpoint = builder["AzureKeyVaultEndpoint"];
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                config.AddAzureKeyVault(keyVaultEndpoint);
            })
            .UseStartup<Startup>()
            .UseKestrel(c => c.AddServerHeader = false)
            .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                //.MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                  //$@"../logscds.txt",
                  $@"D:\home\LogFiles\Application\{Environment.UserDomainName}.txt",
                  fileSizeLimitBytes: 1_000_000,
                  rollOnFileSizeLimit: true,
                  shared: true,
                  flushToDiskInterval: TimeSpan.FromSeconds(1)))
            .Build();

    }
}

