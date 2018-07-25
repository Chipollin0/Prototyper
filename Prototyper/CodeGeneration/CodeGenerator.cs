using CSharpGenerator;
using CSharpGenerator.Syntax;
using CSharpGenerator.Tokens;
using Prototyper.Metadata;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Prototyper.CodeGeneration
{
    // todo: generate using namespaces, depending on what is actually used
    // todo: add support of collections
    // todo: improve setting.Enabled
    // todo: add types: email, domainName, password
    // todo: implement equal for certificates instead of == in properties
    // todo: topology defaults are not always right, sometimes Default==null, need to use some other vlaue.
    // todo: add enabled properties for UI and code behind, so that controls have IsEnabled={Binding something}
    // todo: add namespace name in metadata
    // todo: add generation parameters (generate inactive label)
    // todo: wrap cert actions in trycatch
    // todo: generate NotEmpty property for certificate in case it is used for export
    // todo: generate strings for certificate buttons

    public class CodeGenerator
    {
        public const string indentStep = "    ";

        public static string GenerateSection(ConfigSection section)
        {
            var indent = "";
            var stringBuilder = new StringBuilder();
            GenerateCopyright(stringBuilder);
            GenerateUsings(section, stringBuilder);

            stringBuilder.AppendLine(indent + string.Format("namespace {0}", "ConfigurationUtility.." + section.Name));
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + string.Format("public class {0}Configurator : INotifyPropertyChanged, IDataErrorInfo", section.Name));
            stringBuilder.AppendLine(indent + "{");

            IncreaseIndent(ref indent);
            GenerateConstants(section, stringBuilder, indent);
            GenerateFields(section, stringBuilder, indent);
            GenerateConstructors(section, stringBuilder, indent);
            GenerateProperties(section, stringBuilder, indent);
            GenerateActions(section, stringBuilder, indent);
            GenerateReadWrite(section, stringBuilder, indent);
            GenerateSnapshot(section, stringBuilder, indent);
            GenerateReport(section, stringBuilder, indent);
            GenerateValidation(section, stringBuilder, indent);
            GenerateConfiguratorStateChanged(section, stringBuilder, indent);
            GenerateINotifyPropertyChanged(section, stringBuilder, indent);
            GenerateIDataErrorInfo(section, stringBuilder, indent);
            DecreaseIndent(ref indent);

            stringBuilder.AppendLine(indent + "}");
            
            GenerateEnumClasses(section, stringBuilder, indent);

            DecreaseIndent(ref indent);
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }

        private static void IncreaseIndent(ref string indent)
        {
            indent = indent + indentStep;
        }

        private static void DecreaseIndent(ref string indent)
        {
            if (indent.Length >= indentStep.Length)
                indent = indent.Substring(0, indent.Length - indentStep.Length);
        }

        private static void GenerateCopyright(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(
@"#region Copyright © 2003-2018 Serena Software, Inc. All Rights Reserved
// This file and its contents are protected by United States and 
// International copyright laws. Unauthorized reproduction and/or 
// distribution of all or any portion of the code contained herein 
// is strictly prohibited and will result in severe civil and criminal 
// penalties. Any violations of this copyright will be prosecuted 
// to the fullest extent possible under law. 
// 
// THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE. 
#endregion");
            stringBuilder.AppendLine();
        }

        private static void GenerateUsings(ConfigSection section, StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("using ConfigurationUtility.Core;"); // required always
            stringBuilder.AppendLine("using ConfigurationUtility.Core.FileSystem;");
            stringBuilder.AppendLine("using ConfigurationUtility.Core.Network;"); // used by validate uri
            stringBuilder.AppendLine("using ConfigurationUtility.Sbm;");
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine("using System.Collections.ObjectModel;"); // used by enum
            stringBuilder.AppendLine("using System.ComponentModel;");
            stringBuilder.AppendLine("using System.Linq;"); // used by enum
            stringBuilder.AppendLine();
        }

        private static void GenerateConstants(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            var settings = section.GetSettings();
            foreach (var setting in settings)
                stringBuilder.AppendLine(indent + string.Format("public const string {0}XPath = \"{1}\";", setting.Name, setting.XPath));
            foreach (var setting in settings)
                stringBuilder.AppendLine(indent + string.Format("public const {0} Default{1} = {2};", GetSettingTypeName(setting), setting.Name, GetSettingDefaultValue(setting)));
        }

        private static void GenerateFields(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            var settings = section.GetSettings();
            foreach (var setting in settings)
                GenerateField(setting, stringBuilder, indent);
        }

        private static void GenerateField(ConfigSetting setting, StringBuilder stringBuilder, string indent)
        {
            var settingType = GetSettingTypeName(setting);
            stringBuilder.AppendLine(indent + string.Format("private {0} {1};", settingType, GetLowercase(setting.Name)));

            if (setting.Type == "enum")
            {
                stringBuilder.AppendLine(indent + string.Format("private {0}Item {1}Item;", settingType, GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + string.Format("private ObservableCollection<{0}Item> {1}Items;", settingType, GetLowercase(setting.Name)));
            }
        }

        private static void GenerateConstructors(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            var settings = section.GetSettings();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Constructors");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + string.Format("public {0}Configurator()", GetUppercase(section.Name)));
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            foreach (var setting in settings)
            {
                if (setting.Type == "certificate")
                {
                    stringBuilder.AppendLine(indent + string.Format("{0} = X509CertificateChain.Empty;", setting.Name));
                }

                if (setting.Type == "enum")
                {
                    stringBuilder.AppendLine(indent + string.Format("{0}Items = new ObservableCollection<{0}Item>();", setting.Name));
                    var enumSetting = setting as EnumSetting;
                    if (enumSetting != null)
                    {
                        foreach (var enumOption in enumSetting.Options)
                            stringBuilder.AppendLine(indent + string.Format("{0}Items.Add(new {0}Item{{Key = {1}, Title = StringsResources.{2}, Description = StringsResources.{3}}});", setting.Name, GetSettingValue(setting, enumOption.Name), section.Name + enumOption.Name + "Name", section.Name + enumOption.Name + "Desc"));
                    }

                    //stringBuilder.AppendLine(indent + string.Format("{0}Item = {0}Items.FirstOrDefault();", setting.Name));
                }

                if (setting.Default != null)
                {
                    var name = GetUppercase(setting.Name);
                    var initialize = string.Format("{0} = Default{0};", name);
                    var initializeIndented = Indent(initialize, indent);
                    stringBuilder.AppendLine(initializeIndented);
                }
            }
            stringBuilder.AppendLine(indent + "InitializeConfiguratorStateChanged();");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateProperties(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Properties");
            stringBuilder.AppendLine();
            var settings = section.GetSettings();
            foreach (var setting in settings)
            {
                GenerateProperty(setting, stringBuilder, indent);
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateProperty(ConfigSetting setting, StringBuilder stringBuilder, string indent)
        {
            var settingType = GetSettingTypeName(setting);
            
            if (setting.Type == "enum")
            {
                GenerateProperty(stringBuilder, setting.Name, settingType, indent, string.Format("\r\n{0}{1}Item = {1}Items.FirstOrDefault(i => i.Key == {1});", indent, setting.Name));
                stringBuilder.AppendLine();
                GenerateProperty(stringBuilder, string.Format("{0}Item", setting.Name), string.Format("{0}Item", settingType), indent, string.Format("\r\n{0}{1} = {1}Item.Key;", indent, setting.Name));
                stringBuilder.AppendLine();
                GenerateProperty(stringBuilder, string.Format("{0}Items", settingType), string.Format("ObservableCollection<{0}Item>", setting.Name), indent);
            }
            else
            {
                GenerateProperty(stringBuilder, setting.Name, settingType, indent);
            }
        }

        private static void GenerateProperty(StringBuilder stringBuilder, string name, string type, string indent, string setterExtra = "")
        {
            var template =
@"public $type $uppercase
{
    get { return $lowercase; }
    set
    {
        if ($lowercase == value)
            return;
        $lowercase = value;
        OnPropertyChanged(nameof($uppercase));$setterextra
    }
}";
            var code = template
                .Replace("$type", type)
                .Replace("$uppercase", GetUppercase(name))
                .Replace("$lowercase", GetLowercase(name))
                .Replace("$setterextra", setterExtra);

            var indentedCode = Indent(code, indent);
            stringBuilder.AppendLine(indentedCode);
        }

        private static void GenerateActions(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            var settings = section.GetSettings();
            var certificates = settings.Where(s => s.Type == "certificate").OfType<CertificateSetting>().ToList();
            var anyAction = certificates.Any(c => c.CanGenerate || c.CanImport || c.CanExport || c.CanSelect || c.CanRemove);
            if (anyAction)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(indent + "#region Actions");
                
                foreach(var certificate in certificates)
                {
                    if (certificate.CanGenerate)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(indent + string.Format("public void Generate{0}()", certificate.Name));
                        stringBuilder.AppendLine(indent + "{");
                        IncreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "var chain = SSLManager.GenerateCertificate(false);");
                        stringBuilder.AppendLine(indent + "if (!chain.IsEmpty)");
                        stringBuilder.AppendLine(indent + indentStep + string.Format("{0} = chain;", certificate.Name));
                        DecreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "}");
                    }

                    if (certificate.CanImport)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(indent + string.Format("public void Import{0}()", certificate.Name));
                        stringBuilder.AppendLine(indent + "{");
                        IncreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "var chain = SSLManager.ImportCertificate();");
                        stringBuilder.AppendLine(indent + "if (chain.HasValue)");
                        stringBuilder.AppendLine(indent + indentStep + string.Format("{0} = chain.Value;", certificate.Name));
                        DecreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "}");
                    }

                    if (certificate.CanExport)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(indent + string.Format("public void Export{0}()", certificate.Name));
                        stringBuilder.AppendLine(indent + "{");
                        IncreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + string.Format("SSLConfigUI.ExportChainToFile({0}, \"entry\", ExportChainOptions.AskChainAndKeyOptions);", certificate.Name));
                        DecreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "}");
                    }

                    if (certificate.CanSelect)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(indent + string.Format("public void Select{0}", certificate.Name));
                        stringBuilder.AppendLine(indent + "{");
                        IncreaseIndent(ref indent);
                        DecreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "}");
                    }

                    if (certificate.CanRemove)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(indent + string.Format("public void Remove{0}", certificate.Name));
                        stringBuilder.AppendLine(indent + "{");
                        IncreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + string.Format("{0} = X509CertificateChain.Empty;", certificate.Name));
                        DecreaseIndent(ref indent);
                        stringBuilder.AppendLine(indent + "}");
                    }
                }
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(indent + "#endregion");
            }
        }

        private static void GenerateReadWrite(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Read / Write");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public void ReadConfiguration()");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "var errors = new List<Exception>();");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "try");
            stringBuilder.AppendLine(indent + "{");
            stringBuilder.AppendLine(indent + indentStep + "ReadTopology();");
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine(indent + "catch (Exception e)");
            stringBuilder.AppendLine(indent + "{");
            stringBuilder.AppendLine(indent + indentStep + "errors.Add(e);");
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "if (errors.Any())");
            stringBuilder.AppendLine(indent + indentStep + "throw new AggregateException(errors);");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public void WriteConfiguration()");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "var errors = new List<Exception>();");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "try");
            stringBuilder.AppendLine(indent + "{");
            stringBuilder.AppendLine(indent + indentStep + "WriteTopology();");
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine(indent + "catch (Exception e)");
            stringBuilder.AppendLine(indent + "{");
            stringBuilder.AppendLine(indent + indentStep + "errors.Add(e);");
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "if (errors.Any())");
            stringBuilder.AppendLine(indent + indentStep + "throw new AggregateException(errors);");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");

            // read topology

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "private void ReadTopology()");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "var topology = new TopologyXml();");
            var settings = section.GetSettings();
            foreach (var setting in settings)
            {
                if (setting.Type == "certificate")
                    stringBuilder.AppendLine(indent + string.Format("{0} = topology.GetCertificate(GetTopologyXPath({0}XPath)) ?? Default{0};", setting.Name));
                else if (setting.Type == "bool")
                    stringBuilder.AppendLine(indent + string.Format("{0} = topology.GetBool(GetTopologyXPath({0}XPath)) ?? Default{0};", setting.Name));
                else if(setting.Type == "enum")
                    stringBuilder.AppendLine(indent + string.Format("{0} = EnumExtensions.Parse<{0}>(topology.GetValue(GetTopologyXPath({0}XPath))) ?? Default{0};", setting.Name));
                else
                    stringBuilder.AppendLine(indent + string.Format("{0} = topology.GetValue(GetTopologyXPath({0}XPath)) ?? Default{0};", setting.Name));
            }
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");

            // write topology

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "private void WriteTopology()");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "var topology = new TopologyXml();");
            // var settings = section.GetSettings();
            foreach (var setting in settings)
            {
                if (setting.Type == "certificate")
                    stringBuilder.AppendLine(indent + string.Format("topology.SetCertificate(GetTopologyXPath({0}XPath), {1});", setting.Name, GetSettingStringConversion(setting)));
                else stringBuilder.AppendLine(indent + string.Format("topology.SetValue(GetTopologyXPath({0}XPath), {1});", setting.Name, GetSettingStringConversion(setting)));
            }
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public string GetTopologyXPath(string xpath)");
            stringBuilder.AppendLine(indent + "{");

            stringBuilder.AppendLine(Indent(@"var snapshotPrefixes = new List<string>();
snapshotPrefixes.Add(""Snapshot/"");
snapshotPrefixes.Add(""/Snapshot/"");
foreach (var snapshotPrefix in snapshotPrefixes)
{
    if (xpath.StartsWith(snapshotPrefix))
    {
        xpath = xpath.Remove(0, snapshotPrefix.Length);
        xpath = ""Topology/"" + xpath;
        break;
    }
}

return xpath;", indent + indentStep));

            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateReport(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Report");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public void GenerateReport(ConfigurationReport report)");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + string.Format("var section = StringsResources.{0}ReportSection;", section.Name));
            stringBuilder.AppendLine(indent + string.Format("var subSection = StringsResources.{0}ReportSubSection;", section.Name));
            stringBuilder.AppendLine(indent + "Action<string, string> addReportEntry = (property, value) => report.AddReportEntry(");
            stringBuilder.AppendLine(indent + indentStep + "section, subSection, ReportRowStyle.Normal, property, value);");
            stringBuilder.AppendLine();
            GenerateReportBody(section, stringBuilder, indent, section.Name);
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateReportBody(ConfigBase configBase, StringBuilder stringBuilder, string indent, string sectionName)
        {
            var section = configBase as ConfigSection;
            if (section != null)
            {
                var members = section.Members;
                foreach (var member in members)
                    GenerateReportBody(member, stringBuilder, indent, section.Name);
            }

            var group = configBase as ConfigGroup;
            if (group != null)
            {
                var members = group.Members;
                var groupIndent = indent;
                var condition = group.Enabled;
                if (condition != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", condition));
                    groupIndent = groupIndent + "    ";
                    if (members.Count > 1)
                        stringBuilder.AppendLine(indent + "{");
                }

                foreach (var member in members)
                    GenerateReportBody(member, stringBuilder, groupIndent, sectionName);

                if (members.Count > 1)
                    stringBuilder.AppendLine(indent + "}");
            }

            var setting = configBase as ConfigSetting;
            if (setting != null && SettingUsedInReport(setting))
            {
                if (setting.Enabled != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", setting.Enabled));
                    IncreaseIndent(ref indent);
                }

                var stringConversion = GetSettingStringConversion(setting);
                stringBuilder.AppendLine(indent + string.Format("addReportEntry(StringsResources.{0}Report{1}, {2});", sectionName, setting.Name, stringConversion));

                if (setting.Enabled != null)
                    DecreaseIndent(ref indent);
            }
        }

        private static void GenerateValidation(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Validation");
            
            var settings = section.GetSettings();
            foreach(var setting in settings)
            {
                if (SettingRequiresValidation(setting))
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(indent + string.Format("private string Validate{0}()", setting.Name));
                    stringBuilder.AppendLine(indent + "{");
                    GenerateSettingValidation(setting, stringBuilder, indent + "    ");
                    stringBuilder.AppendLine(indent + "}");
                }
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public bool IsValid()");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            GenerateIsValidBody(section, stringBuilder, indent);
            stringBuilder.AppendLine(indent + "return true;");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateIsValidBody(ConfigBase configBase, StringBuilder stringBuilder, string indent)
        {
            var section = configBase as ConfigSection;
            if (section != null)
            {
                var members = section.Members;
                foreach (var member in members)
                    GenerateIsValidBody(member, stringBuilder, indent);
            }

            var group = configBase as ConfigGroup;
            if (group != null)
            {
                if (group.Enabled != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", group.Enabled));
                    stringBuilder.AppendLine(indent + "{");
                    IncreaseIndent(ref indent);
                }

                var members = group.Members;
                foreach (var member in members)
                    GenerateIsValidBody(member, stringBuilder, indent);

                if (group.Enabled != null)
                {
                    DecreaseIndent(ref indent);
                    stringBuilder.AppendLine(indent + "}");
                }
            }

            var setting = configBase as ConfigSetting;
            if (setting != null && SettingRequiresValidation(setting))
            {
                if (setting.Enabled != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", setting.Enabled));
                    stringBuilder.AppendLine(indent + "{");
                    IncreaseIndent(ref indent);
                }

                stringBuilder.AppendLine(indent + string.Format("if (Validate{0}() != null)", setting.Name));
                stringBuilder.AppendLine(indent + indentStep + "return false;");

                if (setting.Enabled != null)
                {
                    DecreaseIndent(ref indent);
                    stringBuilder.AppendLine(indent + "}");
                }
            }
        }

        private static void GenerateSettingValidation(ConfigSetting setting, StringBuilder stringBuilder, string indent)
        {
            if (setting.Type == "int")
            {
                var min = "0";
                var max = "int.MaxValue";
                var intSetting = setting as IntSetting;
                if (intSetting != null)
                {
                    if (intSetting.Min != null)
                        min = Convert.ToString(intSetting.Min.Value);
                    if (intSetting.Max != null)
                        max = Convert.ToString(intSetting.Max.Value);
                }

                stringBuilder.AppendLine(indent + string.Format("return Validators.ValidateNumber({0}, {1}, {2}, {3});", setting.Name, Convert.ToString(setting.Required).ToLower(), min, max));
                return;
            }

            if (setting.Type == "uri")
            {
                var canBeEmpty = setting.Required ? "false" : "true";
                stringBuilder.AppendLine(indent + string.Format("var value = {0};", setting.Name));
                stringBuilder.AppendLine(indent + string.Format("var error = Url.Validate(value, {0});", canBeEmpty));
                stringBuilder.AppendLine(indent + "return error;");
                return;
            }

            if (setting.Type == "host")
            {
                stringBuilder.AppendLine(indent + string.Format("var value = {0};", setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if (string.IsNullOrEmpty(value))"));
                if (setting.Required)
                    stringBuilder.AppendLine(indent + indentStep + string.Format("return Core.Strings.ValueCannotBeEmpty();"));
                else stringBuilder.AppendLine(indent + indentStep + string.Format("return null;"));
                stringBuilder.AppendLine(indent + "var error = Host.Validate(value, true);");
                stringBuilder.AppendLine(indent + "return error;");
                return;
            }

            if (setting.Type == "port")
            {
                stringBuilder.AppendLine(indent + string.Format("var value = {0};", setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if (string.IsNullOrEmpty(value))"));
                if (setting.Required)
                    stringBuilder.AppendLine(indent + indentStep + string.Format("return Core.Strings.ValueCannotBeEmpty();"));
                else stringBuilder.AppendLine(indent + indentStep + string.Format("return null;"));
                stringBuilder.AppendLine(indent + "var error = new Port(value).Validate();");
                stringBuilder.AppendLine(indent + "return error;");
                return;
            }

            if (setting.Type == "certificate")
            {
                stringBuilder.AppendLine(indent + string.Format("var value = {0};", setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if (value.IsEmpty)"));
                if (setting.Required)
                    stringBuilder.AppendLine(indent + indentStep + string.Format("return Core.Strings.ValueCannotBeEmpty();"));
                else stringBuilder.AppendLine(indent + indentStep + string.Format("return null;"));
                return;
            }

            stringBuilder.AppendLine(indent + "return null;");
        }

        private static void GenerateConfiguratorStateChanged(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region ConfiguratorStateChanged");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Indent(
@"private void InitializeConfiguratorStateChanged()
{
    LoggingContexts.ItemPropertyChanged += (s, a) => OnConfiguratorStateChanged();
    PropertyChanged += (s, a) => OnConfiguratorStateChanged();
}

private void OnConfiguratorStateChanged()
{
    var configurator = UtilityCore.GetConfigurator<>();
    if (IsConfiguratorInitialized(configurator))
        UtilityCore.ConfiguratorStateChanged(configurator);
}

private static bool IsConfiguratorInitialized(ConfiguratorBase configurator)
{
    if (configurator == null)
        return false;
    if (configurator.IsInitializing)
        return false;
    if (!configurator.IsInitialized)
        return false;
    return true;
}", indent));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateSnapshot(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region Snapshot");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public void ReadFromSnapshot(Snapshot snapshot)");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            GenerateReadFromSnapshot(section, stringBuilder, indent);
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public void WriteToSnapshot(Snapshot snapshot)");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            GenerateWriteToSnapshot(section, stringBuilder, indent);
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateReadFromSnapshot(ConfigBase configBase, StringBuilder stringBuilder, string indent)
        {
            var setting = configBase as ConfigSetting;
            if (setting != null)
            {
                GenerateReadFromSnapshot(setting, stringBuilder, indent);
            }

            var section = configBase as ConfigSection;
            if (section != null)
            {
                var members = section.Members;
                foreach (var member in members)
                    GenerateReadFromSnapshot(member, stringBuilder, indent);
            }

            var group = configBase as ConfigGroup;
            if (group != null)
            {
                var members = group.Members;
                foreach (var member in members)
                    GenerateReadFromSnapshot(member, stringBuilder, indent);
            }
        }

        private static void GenerateReadFromSnapshot(ConfigSetting setting, StringBuilder stringBuilder, string indent)
        {
            if (setting.Type == "bool")
            {
                /*stringBuilder.AppendLine(indent + string.Format("var {0}String = snapshot.GetValue({1}XPath, {1});", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if ({0}String != null)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "{");
                stringBuilder.AppendLine(indent + "    " + string.Format("var {0}Parsed = ConvertExtensions.StringToNullableBool({0}String);", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + string.Format("if ({0}Parsed.HasValue)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + "    " + string.Format("{0} = {1}Parsed.Value;", setting.Name, GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "}");*/
                stringBuilder.AppendLine(indent + string.Format("var {0}String = snapshot.GetValue({1}XPath);", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("var {0}Parsed = ConvertExtensions.StringToNullableBool({0}String);", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + string.Format("if ({0}Parsed != null)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + string.Format("{0} = {1}Parsed.Value;", setting.Name, GetLowercase(setting.Name)));
            }
            else if (setting.Type == "enum")
            {
                stringBuilder.AppendLine(indent + string.Format("var {0}String = snapshot.GetValue({1}XPath);", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("var {0}Parsed = EnumExtensions.Parse<{1}>({0}String);", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if ({0}Parsed != null)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + string.Format("{0} = {1}Parsed.Value;", setting.Name, GetLowercase(setting.Name)));
            }
            else if (setting.Type == "certificate")
            {
                stringBuilder.AppendLine(indent + string.Format("var {0}Parsed = snapshot.ReadCertificateChain({1}XPath);", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if ({0}Parsed != null)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + string.Format("{0} = {1}Parsed.Value;", setting.Name, GetLowercase(setting.Name)));
            }
            else
            {
                stringBuilder.AppendLine(indent + string.Format("var {0}String = snapshot.GetValue({1}XPath);", GetLowercase(setting.Name), setting.Name));
                stringBuilder.AppendLine(indent + string.Format("if ({0}String != null)", GetLowercase(setting.Name)));
                stringBuilder.AppendLine(indent + "    " + string.Format("{0} = {1}String;", setting.Name, GetLowercase(setting.Name)));
            }
        }

        private static void GenerateWriteToSnapshot(ConfigBase configBase, StringBuilder stringBuilder, string indent)
        {
            var setting = configBase as ConfigSetting;
            if (setting != null)
            {
                if (setting.Enabled != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", setting.Enabled));
                    //stringBuilder.AppendLine(indent + "{");
                    IncreaseIndent(ref indent);
                }

                if (setting.Type == "certificate")
                {
                    stringBuilder.AppendLine(indent + string.Format("snapshot.WriteCertificateChain({0}, {0}XPath);", GetUppercase(setting.Name)));
                }
                else
                {
                    stringBuilder.AppendLine(indent + string.Format("snapshot.SetValue({0}XPath, {1});", setting.Name, GetSettingStringConversion(setting)));
                }

                if (setting.Enabled != null)
                {
                    DecreaseIndent(ref indent);
                    //stringBuilder.AppendLine(indent + "}");
                }
            }

            var section = configBase as ConfigSection;
            if (section != null)
            {
                var members = section.Members;
                foreach (var member in members)
                    GenerateWriteToSnapshot(member, stringBuilder, indent);
            }

            var group = configBase as ConfigGroup;
            if (group != null)
            {
                var members = group.Members;
                var groupIndent = indent;
                var condition = group.Enabled;
                if (condition != null)
                {
                    stringBuilder.AppendLine(indent + string.Format("if ({0})", condition));
                    groupIndent = groupIndent + "    ";
                    if (members.Count > 1)
                        stringBuilder.AppendLine(indent + "{");
                }

                foreach (var member in members)
                    GenerateWriteToSnapshot(member, stringBuilder, groupIndent);

                if (members.Count > 1)
                    stringBuilder.AppendLine(indent + "}");
            }
        }

        private static void GenerateIDataErrorInfo(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region IDataErrorInfo");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "public string Error { get; }");
            stringBuilder.AppendLine(indent + "public string this[string propertyName]");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "get");
            stringBuilder.AppendLine(indent + "{");
            IncreaseIndent(ref indent);

            var settings = section.GetSettings();
            foreach (var setting in settings)
            {
                if (SettingRequiresValidation(setting))
                {
                    stringBuilder.AppendLine(indent + string.Format("if (propertyName == nameof({0}))", setting.Name));
                    stringBuilder.AppendLine(indent + indentStep + string.Format("return Validate{0}();", setting.Name));
                }
            }

            stringBuilder.AppendLine(indent + string.Format("return null;"));
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateINotifyPropertyChanged(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#region INotifyPropertyChanged");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Indent(
@"public event PropertyChangedEventHandler PropertyChanged;
private void OnPropertyChanged(string property)
{
    var handler = PropertyChanged;
    if (handler != null)
        handler(this, new PropertyChangedEventArgs(property));
}", indent));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(indent + "#endregion");
        }

        private static void GenerateEnumClasses(ConfigSection section, StringBuilder stringBuilder, string indent)
        {
            var settings = section.GetSettings();
            foreach(var setting in settings)
            {
                if (setting.Type == "enum")
                {
                    // the enum itself

                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(indent + string.Format("public enum {0}", setting.Name));
                    stringBuilder.AppendLine(indent + "{");
                    var enumSetting = setting as EnumSetting;
                    if (enumSetting != null)
                    {
                        var enumOptions = enumSetting.Options;
                        foreach(var enumOption in enumOptions)
                            stringBuilder.AppendLine(indent + "    " + enumOption.Name + ",");
                    }
                    stringBuilder.AppendLine(indent + "}");

                    // item class

                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(indent + string.Format("public class {0}Item : INotifyPropertyChanged", setting.Name));
                    stringBuilder.AppendLine(indent + "{");

                    // fields

                    stringBuilder.AppendLine(indent + "    " + string.Format("private {0} {1};", GetSettingTypeName(setting), "key"));
                    stringBuilder.AppendLine(indent + "    " + string.Format("private {0} {1};", "string", "title"));
                    stringBuilder.AppendLine(indent + "    " + string.Format("private {0} {1};", "string", "description"));
                    stringBuilder.AppendLine();

                    // properties

                    stringBuilder.AppendLine(indent + "    " + "#region Properties");
                    stringBuilder.AppendLine();
                    GenerateProperty(stringBuilder, "Key", GetSettingTypeName(setting), indent + "    ");
                    stringBuilder.AppendLine();
                    GenerateProperty(stringBuilder, "Title", "string", indent + "    ");
                    stringBuilder.AppendLine();
                    GenerateProperty(stringBuilder, "Description", "string", indent + "    ");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(indent + "    " + "#endregion");
                    GenerateINotifyPropertyChanged(section, stringBuilder, indent + "    ");
                    stringBuilder.AppendLine(indent + "}");
                }
            }
        }

        private static string Indent(string code, string indent)
        {
            var lines = SplitIntoLines(code);
            for (var i = 0; i < lines.Length; i++)
                lines[i] = indent + lines[i];
            return string.Join(Environment.NewLine, lines);
        }

        private static string[] SplitIntoLines(string text)
        {
            return Regex.Split(text, "\r\n|\r|\n");
        }

        private static string GetUppercase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            var firstCharacter = name[0];
            firstCharacter = char.ToUpperInvariant(firstCharacter);
            if (name.Length == 1)
                return firstCharacter.ToString();
            var result = firstCharacter + name.Substring(1);
            return result;
        }

        private static string GetLowercase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            var firstCharacter = name[0];
            firstCharacter = char.ToLowerInvariant(firstCharacter);
            if (name.Length == 1)
                return firstCharacter.ToString();
            var result = firstCharacter + name.Substring(1);
            return result;
        }

        private static string GetSettingTypeName(ConfigSetting setting)
        {
            if (setting.Type == "host")
                return "string";
            if (setting.Type == "port")
                return "string";
            if (setting.Type == "uri")
                return "string";
            if (setting.Type == "enum")
                return GetUppercase(setting.Name);
            if (setting.Type == "host")
                return "string";
            if (setting.Type == "port")
                return "string";
            if (setting.Type == "int")
                return "string"; // this is counterintuitive
            if (setting.Type == "certificate")
                return "X509CertificateChain";
            return setting.Type;
        }

        private static string GetSettingStringConversion(ConfigSetting setting)
        {
            if (setting.Type == "bool")
                return string.Format("Convert.ToString({0})", GetUppercase(setting.Name));
            //if (setting.Type == "int")
            //    return string.Format("Convert.ToString({0})", GetUppercase(setting.Name));
            if (setting.Type == "enum")
                return string.Format("Convert.ToString({0})", GetUppercase(setting.Name));
            return setting.Name;
        }

        private static string GetSettingDefaultValue(ConfigSetting setting)
        {
            if (setting.Type == "int")
                return string.Format("\"{0}\"", setting.Default);
            if (setting.Type == "uri")
                return string.Format("\"{0}\"", setting.Default);
            if (setting.Type == "enum")
                return string.Format("{0}.{1}", setting.Name, setting.Default);
            if (setting.Type == "host")
                return string.Format("\"{0}\"", setting.Default);
            if (setting.Type == "port")
                return string.Format("\"{0}\"", setting.Default);
            return setting.Default;
        }

        private static string GetSettingValue(ConfigSetting setting, string value)
        {
            if (setting.Type == "int")
                return string.Format("\"{0}\"", value);
            if (setting.Type == "uri")
                return string.Format("\"{0}\"", value);
            if (setting.Type == "enum")
                return string.Format("{0}.{1}", setting.Name, value);
            if (setting.Type == "host")
                return string.Format("\"{0}\"", value);
            if (setting.Type == "port")
                return string.Format("\"{0}\"", value);
            return setting.Default;
        }

        private static bool SettingRequiresValidation(ConfigSetting setting)
        {
            if (setting.Type == "bool")
                return false;
            if (setting.Type == "enum")
                return false;
            if (setting.Type == "certificate" && !setting.Required)
                return false;
            return true;
        }

        private static bool SettingUsedInReport(ConfigSetting setting)
        {
            if (setting.Type == "certificate")
                return false;
            return true;
        }
    }
}
