
using Idp.Swiyu.Passkeys.Sts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddApplicationDbContext(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddSqlServerDbContext<ApplicationDbContext>(connectionName, configureDbContextOptions: options =>
        {
            options.UseSqlServer(o =>
            {
                o.MigrationsAssembly("Idp.Swiyu.Passkeys.Sts.Domain.Migrations");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        builder.EnrichSqlServerDbContext<ApplicationDbContext>();

        builder.Services.AddDbContextFactory<ApplicationDbContext>();

        return builder;
    }
}