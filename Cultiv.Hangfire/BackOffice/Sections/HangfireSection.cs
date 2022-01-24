using Umbraco.Cms.Core.Sections;

namespace Cultiv.Hangfire.BackOffice.Sections
{
    public class HangfireSection : ISection
    {
        public string Alias => Constants.Applications.Hangfire.Alias;

        public string Name => Constants.Applications.Hangfire.Name;
    }
}
