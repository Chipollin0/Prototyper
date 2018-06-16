using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.Metadata
{
    public class ConfigGroup : ConfigBase
    {
        public string Title { get; set; }
        public string Enabled { get; set; }
        public List<ConfigBase> Members { get; set; } = new List<ConfigBase>();
    }
}
