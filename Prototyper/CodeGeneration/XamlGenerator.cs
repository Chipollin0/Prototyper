using Prototyper.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prototyper.CodeGeneration
{
    public static class XamlGenerator
    {
        public const string indentStep = "    ";

        public static string GenerateXaml(ConfigSection section)
        {
            var indent = "";
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<UserControl");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + string.Format("x:Class=\"ConfigurationUtility..{0}", section.Name));
            stringBuilder.AppendLine(indent + "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
            stringBuilder.AppendLine(indent + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            stringBuilder.AppendLine(indent + "xmlns:str=\"clr-namespace:ConfigurationUtility..\"");
            stringBuilder.AppendLine(indent + "xmlns:core=\"clr-namespace:ConfigurationUtility.Core.Wpf\"");
            stringBuilder.AppendLine(indent + "xmlns:sys=\"clr-namespace:System;assembly=mscorlib\"");
            stringBuilder.AppendLine(indent + "Focusable=\"False\"");
            stringBuilder.AppendLine(indent + "UseLayoutRounding=\"True\"");
            stringBuilder.AppendLine(indent + "SnapsToDevicePixels=\"True\"");
            stringBuilder.AppendLine(indent + "RenderOptions.BitmapScalingMode=\"NearestNeighbor\"");
            stringBuilder.AppendLine(indent + "RenderOptions.ClearTypeHint=\"Enabled\"");
            stringBuilder.AppendLine(indent + "TextOptions.TextFormattingMode=\"Display\">");
            stringBuilder.AppendLine(indent + "<UserControl.Resources>");
            IncreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "<sys:Double x:Key=\"Indent\">200</sys:Double>");
            stringBuilder.AppendLine(indent + "<sys:Double x:Key=\"IndentStep\">20</sys:Double>");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "</UserControl.Resources>");
            stringBuilder.AppendLine(indent + "<ScrollViewer HorizontalScrollBarVisibility=\"Disabled\" VerticalScrollBarVisibility=\"Auto\">");
            IncreaseIndent(ref indent);
            GenerateControl(section, section, stringBuilder, indent);
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "</ScrollViewer>");
            DecreaseIndent(ref indent);
            stringBuilder.AppendLine(indent + "</UserControl>");

            return stringBuilder.ToString();
        }

        private static void GenerateControl(ConfigSection parentSection, ConfigBase configBase, StringBuilder stringBuilder, string indent)
        {
            var section = configBase as ConfigSection;
            if (section != null)
            {
                stringBuilder.AppendLine(indent + string.Format("<core:LayoutPanel>"));
                IncreaseIndent(ref indent);

                var members = section.Members;
                foreach (var member in members)
                    GenerateControl(section, member, stringBuilder, indent);

                DecreaseIndent(ref indent);
                stringBuilder.AppendLine(indent + string.Format("</core:LayoutPanel>"));
            }

            var group = configBase as ConfigGroup;
            if (group != null)
            {
                var members = group.Members;
                if (members.Count > 1)
                {
                    if (group.Title != null)
                        stringBuilder.AppendLine(indent + string.Format("<core:GroupBox Header=\"{0}\" core:Properties.IndentLeft=\"{{StaticResource IndentStep}}\">", group.Title));
                    else stringBuilder.AppendLine(indent + string.Format("<core:LayoutPanel core:Properties.IndentLeft=\"{{StaticResource IndentStep}}>"));
                    IncreaseIndent(ref indent);
                }
                
                foreach (var member in members)
                    GenerateControl(parentSection, member, stringBuilder, indent);

                if (members.Count > 1)
                {
                    DecreaseIndent(ref indent);
                    if (group.Title != null)
                        stringBuilder.AppendLine(indent + string.Format("</core:GroupBox>"));
                    else stringBuilder.AppendLine(indent + string.Format("</core:LayoutPanel>"));
                }
            }

            var setting = configBase as ConfigSetting;
            if (setting != null)
            {
                if (setting.Type == "bool")
                    stringBuilder.AppendLine(indent + string.Format("<core:CheckBox Text=\"{{x:Static str:StringsResources.{0}}}\" Checked=\"{{Binding {1}}}\" Indent=\"{{StaticResource Indent}}\" HorizontalAlignment=\"Left\"/>", parentSection.Name + setting.Name, setting.Name));
                if (setting.Type == "int")
                    stringBuilder.AppendLine(indent + string.Format("<core:TextBox Header=\"{{x:Static str:StringsResources.{0}}}\" Text=\"{{Binding {1},ValidatesOnDataErrors=True,UpdateSourceTrigger=PropertyChanged}}\" Indent=\"{{StaticResource Indent}}\" HorizontalAlignment=\"Left\" TextAreaWidth=\"150\"/>", parentSection.Name + setting.Name, setting.Name));
                if (setting.Type == "host")
                    stringBuilder.AppendLine(indent + string.Format("<core:TextBox Header=\"{{x:Static str:StringsResources.{0}}}\" Text=\"{{Binding {1},ValidatesOnDataErrors=True,UpdateSourceTrigger=PropertyChanged}}\" Indent=\"{{StaticResource Indent}}\" HorizontalAlignment=\"Left\" TextAreaWidth=\"150\"/>", parentSection.Name + setting.Name, setting.Name));
                if (setting.Type == "port")
                    stringBuilder.AppendLine(indent + string.Format("<core:TextBox Header=\"{{x:Static str:StringsResources.{0}}}\" Text=\"{{Binding {1},ValidatesOnDataErrors=True,UpdateSourceTrigger=PropertyChanged}}\" Indent=\"{{StaticResource Indent}}\" HorizontalAlignment=\"Left\" TextAreaWidth=\"150\"/>", parentSection.Name + setting.Name, setting.Name));
                if (setting.Type == "uri")
                    stringBuilder.AppendLine(indent + string.Format("<core:TextBox Header=\"{{x:Static str:StringsResources.{0}}}\" Text=\"{{Binding {1},ValidatesOnDataErrors=True,UpdateSourceTrigger=PropertyChanged}}\" Indent=\"{{StaticResource Indent}}\" HorizontalAlignment=\"Left\" TextAreaWidth=\"150\"/>", parentSection.Name + setting.Name, setting.Name));
                if (setting.Type == "enum")
                    stringBuilder.AppendLine(indent + string.Format("<core:ComboBox Header=\"{{x:Static str:StringsResources.{0}}}\" SelectedItem=\"{{Binding {1}}}\" Items=\"{{Binding {2}}}\" VerticalAlignment=\"Top\"/>", parentSection.Name + setting.Name, setting.Name + "Item", setting.Name + "Items"));
                if (setting.Type == "certificate")
                {
                    var certificate = (CertificateSetting)setting;
                    stringBuilder.AppendLine(indent + string.Format("<core:LayoutPanel Orientation=\"Horizontal\">"));
                    IncreaseIndent(ref indent);
                    stringBuilder.AppendLine(indent + string.Format("<core:CertificateControl CertificateChain=\"{{Binding {0}, ValidatesOnDataErrors=True}}\" Width=\"450\"/>", setting.Name));
                    stringBuilder.AppendLine(indent + string.Format("<core:LayoutPanel>"));
                    IncreaseIndent(ref indent);
                    if (certificate.CanGenerate)
                        stringBuilder.AppendLine(indent + string.Format("<Button Content=\"{{x:Static str:StringsResources.{0}Generate{1}Button}}\" Style=\"{{DynamicResource SimpleButton}}\" Click=\"OnGenerate{1}Click\"/>", parentSection.Name, setting.Name));
                    if (certificate.CanImport)
                        stringBuilder.AppendLine(indent + string.Format("<Button Content=\"{{x:Static str:StringsResources.{0}Import{1}Button}}\" Style=\"{{DynamicResource SimpleButton}}\" Click=\"OnImport{1}Click\"/>", parentSection.Name, setting.Name));
                    if (certificate.CanExport)
                        stringBuilder.AppendLine(indent + string.Format("<Button Content=\"{{x:Static str:StringsResources.{0}Export{1}Button}}\" Style=\"{{DynamicResource SimpleButton}}\" Click=\"OnExport{1}Click\" IsEnabled=\"{{Binding {1}NotEmpty}}\"/>", parentSection.Name, setting.Name));
                    if (certificate.CanSelect)
                        stringBuilder.AppendLine(indent + string.Format("<Button Content=\"{{x:Static str:StringsResources.{0}Select{1}Button}}\" Style=\"{{DynamicResource SimpleButton}}\" Click=\"OnSelect{1}Click\"/>", parentSection.Name, setting.Name));
                    if (certificate.CanRemove)
                        stringBuilder.AppendLine(indent + string.Format("<Button Content=\"{{x:Static str:StringsResources.{0}Remove{1}Button}}\" Style=\"{{DynamicResource SimpleButton}}\" Click=\"OnRemove{1}Click\" IsEnabled=\"{{Binding {1}NotEmpty}}\"/>", parentSection.Name, setting.Name));
                    DecreaseIndent(ref indent);
                    stringBuilder.AppendLine(indent + string.Format("</core:LayoutPanel>"));
                    DecreaseIndent(ref indent);
                    stringBuilder.AppendLine(indent + string.Format("</core:LayoutPanel>"));
                }
            }
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
    }
}
