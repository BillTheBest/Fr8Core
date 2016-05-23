using Utilities.Configuration;

namespace HubTests.Services
{
    public class DisableCacheSettings : ConfigurationOverride
    {
        public DisableCacheSettings(IApplicationSettings @base)
            :base (@base)
        {
            Set("DisableATandTCache", "true");
        }
    }
}