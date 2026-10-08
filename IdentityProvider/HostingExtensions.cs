// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.ResponseHandling;
using Idp.Swiyu.Passkeys.Sts.Domain;
using Idp.Swiyu.Passkeys.Sts.Domain.Models;
using Idp.Swiyu.Passkeys.Sts.Passkeys;
using Idp.Swiyu.Passkeys.Sts.Services;
using Idp.Swiyu.Passkeys.Sts.SwiyuServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;
using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Idp.Swiyu.Passkeys.Sts;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Write most logs to the console but diagnostic data to a file.
        // See https://docs.duendesoftware.com/identityserver/diagnostics/data
        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture);
                if (builder.Environment.IsDevelopment())
                {
                    consoleLogger.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            if (builder.Environment.IsDevelopment())
            {
                lc.WriteTo.Logger(fileLogger =>
                {
                    fileLogger
                        .WriteTo.File("./diagnostics/diagnostic.log", rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                            rollOnFileSizeLimit: true,
                            outputTemplate:
                            "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                            formatProvider: CultureInfo.InvariantCulture)
                        .Filter
                        .ByIncludingOnly(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }).Enrich.FromLogContext().ReadFrom.Configuration(ctx.Configuration);
            }
        });
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var stsSigningPrivatePem = ConfigConverter.GetPemFromBase64Config("StsSigningPrivatePemBase64", builder.Configuration);
        var stsSigningPublicPem = ConfigConverter.GetPemFromBase64Config("StsSigningPublicPemBase64", builder.Configuration);

        var ecdsaCertificate = X509Certificate2.CreateFromPem(stsSigningPublicPem, stsSigningPrivatePem);
        var ecdsaCertificateKey = new ECDsaSecurityKey(ecdsaCertificate.GetECDsaPrivateKey());

        builder.Services.AddScoped<VerificationService>();

        builder.Services.AddHttpClient();
        builder.Services.AddOptions();
        builder.Services.AddRazorPages();

        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.AddTransient<IEmailSender, EmailSender>();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("database")));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var url = new Uri(builder.Configuration["WebOidcAuthority"]!);

        builder.Services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ValidateOrigin = async (context) =>
            {
                if (context.Origin == url.OriginalString)
                {
                    return true;
                }

                return false;
            };
        });

        builder.Services.AddTransient<IAuthorizeInteractionResponseGenerator, StepUpInteractionResponseGenerator>();

        var idsvrBuilder = builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.KeyManagement.Enabled = false;
                // Use a large chunk size for diagnostic data in development where it will be redirected to a local file.
                if (builder.Environment.IsDevelopment())
                {
                    options.Diagnostics.ChunkSize = 1024 * 1024 * 10; // 10 MB
                }

                // see https://docs.duendesoftware.com/identityserver/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;

                options.ServerSideSessions.UserDisplayNameClaimType = "name"; // this sets the "name" claim as the display name in the admin tool
                options.ServerSideSessions.RemoveExpiredSessions = true; // removes expired sessions. defaults to true.
                options.ServerSideSessions.ExpiredSessionsTriggerBackchannelLogout = true; // this triggers notification to clients. defaults to false

            })
            .AddSigningCredential(ecdsaCertificateKey, "ES256") // ecdsaCertificate
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.GetApiScopes())
            .AddInMemoryClients(Config.GetClients())
            .AddInMemoryApiResources(Config.GetApiResources())
            .AddAspNetIdentity<ApplicationUser>()
            .AddLicenseSummary()
            .AddProfileService<ProfileService>()
            // enables server-side sessions
            .AddServerSideSessions();

        idsvrBuilder.AddJwtBearerClientAuthentication();

        builder.Services.AddHealthChecks();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapPasskeyEndpoints();

        app.MapRazorPages()
            .RequireAuthorization();

        app.MapHealthChecks("/health")
            .AllowAnonymous();

        return app;
    }
}
