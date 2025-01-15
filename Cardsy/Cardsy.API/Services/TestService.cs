using Cardsy.API.Options;
using Microsoft.Extensions.Options;

namespace Cardsy.API.Services
{
    public class TestService(IOptionsMonitor<Configuration> options)
    {
        private readonly IOptionsMonitor<Configuration> _configuration = options;

        public Setting[] Settings { 
            get
            {
                return typeof(Configuration).GetProperties().Select(p => new Setting(p.Name, p.GetValue(_configuration.CurrentValue)?.ToString())).ToArray();
            }
        }

        public Setting GetSetting(string key)
        {
            return new Setting(key, typeof(Configuration).GetProperty(key)?.GetValue(_configuration.CurrentValue)?.ToString());
        }
    }
}
