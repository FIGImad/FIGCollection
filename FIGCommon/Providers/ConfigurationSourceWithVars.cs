using Microsoft.Extensions.Configuration;

namespace FIGCommon.Providers
{
    public class ConfigurationSourceWithVars : IConfigurationSource
    {
        private readonly IConfiguration _baseConfiguration;

        public ConfigurationSourceWithVars(IConfiguration baseConfiguration)
        {
            _baseConfiguration = baseConfiguration;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ConfigurationProviderWithVars(_baseConfiguration);
        }
    }
}
