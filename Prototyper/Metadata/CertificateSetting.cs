using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.Metadata
{
    public class CertificateSetting : ConfigSetting
    {
        public CertificateSetting()
        {
            IsSsl = false;
            CanExport = true;
            CanImport = true;
            CanGenerate = true;
            CanSelect = false;
            CanRemove = false;
            RequirePrivateKey = true;
        }

        public bool IsSsl { get; set; }
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }
        public bool CanGenerate { get; set; }
        public bool CanSelect { get; set; }
        public bool CanRemove { get; set; }
        public bool RequirePrivateKey { get; set; }
    }
}
