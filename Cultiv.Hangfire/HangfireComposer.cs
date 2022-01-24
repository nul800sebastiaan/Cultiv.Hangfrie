using System;
using Cultiv.Hangfire.BackOffice.Sections;
using Cultiv.Hangfire.Configuration;
using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.BackOffice.Authorization;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Cultiv.Hangfire
{
    public class HangfireComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register configuration
            var options = builder.Services.AddOptions<HangfireSettings>()
                .Bind(builder.Config.GetSection(Constants.System.ProductName))
                .ValidateDataAnnotations();

            // TODO: Can we add this based on config?
            builder.Sections().Append<HangfireSection>();

            // Configure Hangfire to use our current database and add the option to write console messages
            var connectionString = builder.Config.GetConnectionString(Umbraco.Cms.Core.Constants.System.UmbracoConnectionName);
            if (string.IsNullOrEmpty(connectionString) == false)
            {
                builder.Services.AddHangfire(configuration =>
                {
                    configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseConsole()
                        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            QueuePollInterval = TimeSpan.Zero,
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true,
                        });
                });

                // Run the required server so your queued jobs will get executed
                builder.Services.AddHangfireServer();

                AddAuthorizedUmbracoDashboard(builder);
                
                // For some reason we need to give it the connection string again, else we get this error:
                // https://discuss.hangfire.io/t/jobstorage-current-property-value-has-not-been-initialized/884
                JobStorage.Current = new SqlServerStorage(connectionString);
            }
        }

        private static void AddAuthorizedUmbracoDashboard(IUmbracoBuilder builder)
        {
            // Add a named policy to authorize requests to the dashboard
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.System.HangfireDashboard, policy =>
                {
                    // We require a logged in backoffice user who has access to the settings section
                    policy.AuthenticationSchemes.Add(Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType);
                    policy.Requirements.Add(new SectionRequirement(Umbraco.Cms.Core.Constants.Applications.Settings));
                });
            });

            // Add the dashboard and make sure it's authorized with the named policy above
            builder.Services.Configure<UmbracoPipelineOptions>(options =>
            {
                options.AddFilter(new UmbracoPipelineFilter(Constants.System.HangfireDashboard)
                {
                    Endpoints = app => app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHangfireDashboard(
                            pattern: "/umbraco/backoffice/hangfire",
                            options: new DashboardOptions
                            {
                                AppPath = null // Hide "Back to site" link
                            }
                        )
                        .RequireAuthorization(Constants.System.HangfireDashboard);
                    })
                    .UseHangfireDashboard()
                });
            });
        }
    }
}