using Prototyper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Prototyper.Serialization
{
    public class MetadataSerializer
    {
        public static ConfigSection LoadSection(XmlDocument xmlDocument)
        {
            var section = new ConfigSection();
            section.Name = GetAttribute(xmlDocument.DocumentElement, "name", null);
            section.Members = LoadMembers(xmlDocument.DocumentElement);
            return section;
        }

        public static XmlDocument SaveSection(ConfigSection section)
        {
            return new XmlDocument();
        }

        public static ConfigSetting LoadSetting(XmlElement xmlElement)
        {
            var configSetting = new ConfigSetting();
            var type = GetAttribute(xmlElement, "type", null);

            if (type == "enum")
            {
                var enumSetting = new EnumSetting();
                var configOptionElements = xmlElement.SelectNodes("./ConfigOption").OfType<XmlElement>();
                foreach(var configOptionElement in configOptionElements)
                {
                    var enumOption = new EnumOption();
                    enumOption.Name = GetAttribute(configOptionElement, "name", null);
                    enumOption.Title = GetAttribute(configOptionElement, "title", null);
                    enumOption.Desc = GetAttribute(configOptionElement, "desc", null);
                    enumSetting.Options.Add(enumOption);
                }

                configSetting = enumSetting;
            }

            if (type == "int")
            {
                var intSetting = new IntSetting();
                intSetting.Min = GetAttribute(xmlElement, "min");
                intSetting.Max = GetAttribute(xmlElement, "max");
                configSetting = intSetting;
            }

            configSetting.Name = GetAttribute(xmlElement, "name", null);
            configSetting.Default = GetAttribute(xmlElement, "default", null);
            configSetting.Enabled = GetAttribute(xmlElement, "enabled", null);
            configSetting.Required = Convert.ToBoolean(GetAttribute(xmlElement, "required", "false"));
            configSetting.Title = GetAttribute(xmlElement, "title", null);
            configSetting.Type = GetAttribute(xmlElement, "type", null);
            configSetting.XPath = GetAttribute(xmlElement, "xpath", null);
            return configSetting;
        }

        public static ConfigGroup LoadGroup(XmlElement xmlElement)
        {
            var configGroup = new ConfigGroup();
            configGroup.Enabled = GetAttribute(xmlElement, "enabled", null);
            configGroup.Members = LoadMembers(xmlElement);
            return configGroup;
        }

        private static List<ConfigBase> LoadMembers(XmlElement xmlElement)
        {
            var members = new List<ConfigBase>();
            var childElements = xmlElement.SelectNodes("./*").OfType<XmlElement>();
            foreach (var childElement in childElements)
            {
                if (childElement.LocalName == "ConfigSetting")
                {
                    var configSetting = LoadSetting(childElement);
                    members.Add(configSetting);
                }

                if (childElement.LocalName == "ConfigGroup")
                {
                    var configGroup = LoadGroup(childElement);
                    members.Add(configGroup);
                }
            }

            return members;
        }

        private static string GetAttribute(XmlElement xmlElement, string attribute, string defaultValue)
        {
            if (xmlElement.HasAttribute(attribute))
                return xmlElement.GetAttribute(attribute);
            return defaultValue;
        }

        private static int? GetAttribute(XmlElement xmlElement, string attribute)
        {
            if (xmlElement.HasAttribute(attribute))
            {
                var valueString = xmlElement.GetAttribute(attribute);
                var valueInt = 0;
                var success = int.TryParse(valueString, out valueInt);
                if (success)
                    return valueInt;
            }
            return null;
        }
    }
}
