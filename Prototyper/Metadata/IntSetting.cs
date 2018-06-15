using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.Metadata
{
    public class IntSetting : ConfigSetting
    {
        public int? Min { get; set; }
        public int? Max { get; set; }
    }
}
