using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.Metadata
{
    public class ConfigSetting : ConfigBase
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Default { get; set; }
        public bool Required { get; set; }
        public string XPath { get; set; }
        public string Enabled { get; set; }
    }
}
