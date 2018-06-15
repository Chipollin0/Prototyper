using Prototyper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.CodeGeneration
{
    public static class StringsGenerator
    {
        public const string indentStep = "  ";

        public static string GenerateStrings(ConfigSection section)
        {
            var stringBuilder = new StringBuilder();
            var settings = section.GetSettings();

            foreach (var setting in settings)
            {
                var id = string.Format("{0}{1}", section.Name, setting.Name);
                GenerateString(stringBuilder, id, setting.Title);

            }

            foreach (var setting in settings)
            {
                var id = string.Format("{0}Report{1}", section.Name, setting.Name);
                GenerateString(stringBuilder, id, setting.Title);

            }

            return stringBuilder.ToString();
        }

        private static void GenerateString(StringBuilder stringBuilder, string id, string message)
        {
            var xml =
                "  <data name=\"{0}\" xml:space=\"preserve\">\r\n" +
                "    <value>{1}</value>\r\n" +
                "  </data>";
            var result = string.Format(xml, id, message);
            stringBuilder.AppendLine(result);
        }
    }
}
