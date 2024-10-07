using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace Cultiv.Hangfire;

public class HangfireComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var serverDisabled = false;
        var settings = builder.Config.GetSection("Hangfire").Get<HangfireSettings>();
        if (settings != null && settings.Server != null)
        {
            serverDisabled = settings.Server.Disabled.GetValueOrDefault(defaultValue: false);
        }

        builder.ManifestFilters().Append<ManifestFilter>();

        var provider = builder.Config.GetConnectionStringProviderName(Umbraco.Cms.Core.Constants.System.UmbracoConnectionName);

        if (provider.InvariantEquals("Microsoft.Data.SQLite"))
        {
            GlobalConfiguration.Configuration.UseSQLiteStorage();

            builder.Services.AddHangfire(configuration =>
            {
                configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseColouredConsoleLogProvider()
                    .UseDashboardMetric(SqlServerStorage.ActiveConnections)
                    .UseDashboardMetric(SqlServerStorage.TotalConnections)
                    .UseDashboardMetric(DashboardMetrics.AwaitingCount)
                    .UseDashboardMetric(DashboardMetrics.FailedCount)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseConsole();
            });

            if (!serverDisabled)
            {
                // Run the required server so your queued jobs will get executed
                builder.Services.AddHangfireServer();
            }

            AddAuthorizedUmbracoDashboard(builder);

            return;
        }

        var connectionString = builder.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            // This might happen when the package is installed before Umbraco is installed
            // https://github.com/nul800sebastiaan/Cultiv.Hangfire/issues/11
            return;
        }

        // Explicitly use the SqlConnection in the Microsoft.Data namespace to support extended connection string parameters such as "authentication"
        // https://github.com/HangfireIO/Hangfire/issues/1827
        SqlConnection ConnectionFactory() => new(connectionString);

        // Configure Hangfire to use our current database and add the option to write console messages
        builder.Services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseColouredConsoleLogProvider()
                .UseDashboardMetric(SqlServerStorage.ActiveConnections)
                .UseDashboardMetric(SqlServerStorage.TotalConnections)
                .UseDashboardMetric(DashboardMetrics.AwaitingCount)
                .UseDashboardMetric(DashboardMetrics.FailedCount)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseConsole()
                .UseSqlServerStorage((Func<SqlConnection>)ConnectionFactory, new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = settings.StorageOptions.PrepareSchemaIfNecessary,
                    EnableHeavyMigrations = settings.StorageOptions.EnableHeavyMigrations,
                    CommandBatchMaxTimeout = settings.StorageOptions.CommandBatchMaxTimeout,
                    SlidingInvisibilityTimeout = settings.StorageOptions.SlidingInvisibilityTimeout,
                    QueuePollInterval = settings.StorageOptions.QueuePollInterval,
                    UseRecommendedIsolationLevel = settings.StorageOptions.UseRecommendedIsolationLevel,
                    DisableGlobalLocks = settings.StorageOptions.DisableGlobalLocks
                });
        });

        if (!serverDisabled)
        {
            // Run the required server so your queued jobs will get executed
            builder.Services.AddHangfireServer();
        }

        AddAuthorizedUmbracoDashboard(builder);
        // For some reason we need to give it the connection string again, else we get this error:
        // https://discuss.hangfire.io/t/jobstorage-current-property-value-has-not-been-initialized/884
        JobStorage.Current = new SqlServerStorage((Func<SqlConnection>)ConnectionFactory);
    }

    private static void AddAuthorizedUmbracoDashboard(IUmbracoBuilder builder)
    {
        // Add the dashboard and make sure it's authorized with the named policy above
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(Constants.System.HangfireDashboard)
            {
                Endpoints = app => app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHangfireDashboardWithAuthorizationPolicy(
                        pattern: Constants.System.Endpoint,
                        options: new DashboardOptions { IgnoreAntiforgeryToken = true },
                        authorizationPolicyName: AuthorizationPolicies.SectionAccessSettings);
                })
            });
        });
    }
}