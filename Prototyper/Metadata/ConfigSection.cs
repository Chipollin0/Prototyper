using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.Metadata
{
    public class ConfigSection : ConfigBase
    {
        public string Name { get; set; }
        public List<ConfigBase> Members { get; set; } = new List<ConfigBase>();

        public IEnumerable<ConfigSetting> GetSettings()
        {
            return GetSettings(this);
        }

        private IEnumerable<ConfigSetting> GetSettings(ConfigBase configBase)
        {
            if (configBase is ConfigSetting)
                yield return (ConfigSetting)configBase;

            if (configBase is ConfigGroup)
            {
                var group = (ConfigGroup)configBase;
                var members = group.Members;
                foreach(var member in members)
                {
                    var settings = GetSettings(member);
                    foreach (var setting in settings)
                        yield return setting;
                }
            }

            if (configBase is ConfigSection)
            {
                var group = (ConfigSection)configBase;
                var members = group.Members;
                foreach (var member in members)
                {
                    var settings = GetSettings(member);
                    foreach (var setting in settings)
                        yield return setting;
                }
            }
        }
    }
}
