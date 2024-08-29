using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace ConsoleStandaloneUsingAzureSecrets;

class Program
{
    private static IConfigurationRoot _config;
    private static IServiceProvider _services;

    static void Main(string[] args)
    {
        Console.WriteLine("Start Application and get key vault values");

        GetConfigurationsForEnvironment();

        Console.WriteLine("Read Configurations");

        SetupServices();

        Console.WriteLine("Services ready");

        // read config value
        var someSecret = _config["SomeSecret"];

        Console.WriteLine($"Read from configuration: {someSecret}");
        Console.ReadLine();
    }

    private static void SetupServices()
    {
        var serviceCollection = new ServiceCollection();

        // Do migration, seeding logic or whatever

        _services = serviceCollection.BuildServiceProvider();
    }

    private static void GetConfigurationsForEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var location = Assembly.GetEntryAssembly().Location;
        var directory = Path.GetDirectoryName(location);

        Console.WriteLine($"appsettings.json found");
        Console.WriteLine($"{environmentName}");

        var configBuilder = new ConfigurationBuilder()
       .AddJsonFile($"{directory}{Path.DirectorySeparatorChar}appsettings.json", false, true)
       .AddJsonFile($"{directory}{Path.DirectorySeparatorChar}appsettings.{environmentName}.json", true, true)
       .AddEnvironmentVariables();
        _config = configBuilder.Build();

        var dnsNameKeyVault = _config["DNSNameKeyVault"];

        if (!string.IsNullOrWhiteSpace(dnsNameKeyVault))
        {
            configBuilder.AddAzureKeyVault(new Uri(dnsNameKeyVault), new DefaultAzureCredential());
            _config = configBuilder.Build();
        }
    }
}