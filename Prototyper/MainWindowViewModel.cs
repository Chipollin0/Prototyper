using Prototyper.CodeGeneration;
using Prototyper.Serialization;
using System;
using System.ComponentModel;
using System.Xml;

namespace Prototyper
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string metadataXml;
        private string outputCode;
        private string outputXaml;
        private string outputUnitTests;
        private string outputStrings;

        #region Constructors

        public MainWindowViewModel()
        {
            MetadataXml = @"<ConfigSection name=""SecureHeaders"">
  <ConfigSetting name=""HeadersEnabled"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/HeadersEnabled"" default=""false"" title=""Enable HTTP security response headers""/>
  <ConfigGroup enabled=""HeadersEnabled"">
    <ConfigSetting name=""HstsEnabled"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/HstsEnabled"" default=""true"" title=""Enable HTTP strict transport security (HSTS)""/>
    <ConfigGroup enabled=""HstsEnabled"">
      <ConfigSetting name=""HstsMaxAgeSeconds"" type=""int"" xpath=""Snapshot/Security/SecureHeaders/HstsMaxAgeSeconds"" default=""31536000"" min=""0"" required=""true"" title=""Max age (in seconds)""/>
      <ConfigSetting name=""HstsIncludeSubDomains"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/HstsIncludeSubDomains"" default=""false"" title=""Include sub domains""/>
    </ConfigGroup>
    <ConfigSetting name=""AntiClickJackingEnabled"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/AntiClickJackingEnabled"" default=""true"" title=""Enable anti click-jacking options""/>
    <ConfigGroup enabled=""AntiClickJackingEnabled"">
      <ConfigSetting name=""AntiClickJackingOption"" type=""enum"" xpath=""Snapshot/Security/SecureHeaders/AntiClickJackingOption"" default=""SameOrigin"" title=""Load page in frame"">
        <ConfigOption name=""Deny"" title=""Deny"" desc=""The page cannot be displayed in a frame, regardless of the site attempting to do so.""/>
        <ConfigOption name=""SameOrigin"" title=""Allow from the same origin"" desc=""The page can only be displayed in a frame on the same origin as the page itself.""/>
        <ConfigOption name=""AllowFromUri"" title=""Allow from specified URI"" desc=""The page can only be displayed in a frame on the specified origin.""/>
      </ConfigSetting>
      <ConfigSetting name=""AntiClickJackingUri"" type=""uri"" xpath=""Snapshot/Security/SecureHeaders/AntiClickJackingUri"" default="""" required=""true"" enabled=""AntiClickJackingOption=AllowFromUri"" title=""URI""/>
    </ConfigGroup>
    <ConfigSetting name=""ContentSniffingBlocked"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/ContentSniffingBlocked"" default=""true"" title=""Block content sniffing""/>
    <ConfigSetting name=""XssProtectionEnabled"" type=""bool"" xpath=""Snapshot/Security/SecureHeaders/XssProtectionEnabled"" default=""true"" title=""Enable XSS (cross-site scripting) filtering""/>
  </ConfigGroup>
</ConfigSection>";
        }

        #endregion

        #region Properties

        public string MetadataXml
        {
            get { return metadataXml; }
            set
            {
                if (metadataXml == value)
                    return;
                metadataXml = value;
                OnPropertyChanged(nameof(MetadataXml));
                UpdateOutputCode();
                UpdateOutputStrings();
                UpdateOutputXaml();
            }
        }

        public string OutputCode
        {
            get { return outputCode; }
            set
            {
                if (outputCode == value)
                    return;
                outputCode = value;
                OnPropertyChanged(nameof(OutputCode));
            }
        }

        public string OutputXaml
        {
            get { return outputXaml; }
            set
            {
                if (outputXaml == value)
                    return;
                outputXaml = value;
                OnPropertyChanged(nameof(OutputXaml));
            }
        }

        public string OutputUnitTests
        {
            get { return outputUnitTests; }
            set
            {
                if (outputUnitTests == value)
                    return;
                outputUnitTests = value;
                OnPropertyChanged(nameof(OutputUnitTests));
            }
        }

        public string OutputStrings
        {
            get { return outputStrings; }
            set
            {
                if (outputStrings == value)
                    return;
                outputStrings = value;
                OnPropertyChanged(nameof(OutputStrings));
            }
        }

        private void UpdateOutputCode()
        {
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(metadataXml);
                var configSection = MetadataSerializer.LoadSection(xmlDocument);
                OutputCode = CodeGenerator.GenerateSection(configSection);
            }
            catch (Exception e)
            {
                OutputCode = e.ToString();
            }
        }

        private void UpdateOutputStrings()
        {
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(metadataXml);
                var configSection = MetadataSerializer.LoadSection(xmlDocument);
                OutputStrings = StringsGenerator.GenerateStrings(configSection);
            }
            catch (Exception e)
            {
                OutputStrings = e.ToString();
            }
        }

        private void UpdateOutputXaml()
        {
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(metadataXml);
                var configSection = MetadataSerializer.LoadSection(xmlDocument);
                OutputXaml = XamlGenerator.GenerateXaml(configSection);
            }
            catch (Exception e)
            {
                OutputXaml = e.ToString();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(property));
        }

        #endregion
    }
}
