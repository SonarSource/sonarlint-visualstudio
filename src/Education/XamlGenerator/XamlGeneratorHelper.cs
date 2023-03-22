/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Xml;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    internal interface IXamlGeneratorHelper
    {
        void WriteDocumentHeader(IRuleInfo ruleInfo);
        void EndDocument();
    }

    internal interface IXamlGeneratorHelperFactory
    {
        IXamlGeneratorHelper Create(XmlWriter writer);
    }

    internal class XamlGeneratorHelperFactory : IXamlGeneratorHelperFactory
    {
        private readonly IRuleHelpXamlTranslator ruleHelpXamlTranslator;

        public XamlGeneratorHelperFactory(IRuleHelpXamlTranslator ruleHelpXamlTranslator)
        {
            this.ruleHelpXamlTranslator = ruleHelpXamlTranslator;
        }

        public IXamlGeneratorHelper Create(XmlWriter writer)
        {
            return new XamlGeneratorHelper(writer, ruleHelpXamlTranslator);
        }

        private sealed class XamlGeneratorHelper : IXamlGeneratorHelper
        {
            private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            private readonly XmlWriter writer;
            private readonly IRuleHelpXamlTranslator ruleHelpXamlTranslator;

            public XamlGeneratorHelper(XmlWriter writer, IRuleHelpXamlTranslator ruleHelpXamlTranslator)
            {
                this.writer = writer;
                this.ruleHelpXamlTranslator = ruleHelpXamlTranslator;
            }

            public void WriteDocumentHeader(IRuleInfo ruleInfo)
            {
                writer.WriteStartElement("FlowDocument", XamlNamespace);
                writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

                WriteTitle(ruleInfo.Name);
                WriteSubTitle(ruleInfo);
                WriteExtendedDescriptionIfPresent(ruleInfo);
            }

            public void EndDocument()
            {
                writer.WriteEndElement();
                writer.Close();
            }

            private void WriteTitle(string text)
            {
                writer.WriteStartElement("Paragraph");
                writer.ApplyStyleToElement(StyleResourceNames.Title_Paragraph);
                writer.WriteString(text);
                writer.WriteEndElement();
            }

            private void WriteExtendedDescriptionIfPresent(IRuleInfo ruleInfo)
            {
                if (string.IsNullOrWhiteSpace(ruleInfo.HtmlNote))
                {
                    return;
                }

                writer.WriteRaw(ruleHelpXamlTranslator.TranslateHtmlToXaml(ruleInfo.HtmlNote));
            }

            private void WriteSubTitle(IRuleInfo ruleInfo)
            {
                writer.WriteStartElement("Paragraph");
                writer.ApplyStyleToElement(StyleResourceNames.Title_Paragraph);

                WriteSubTitleElement_IssueType(ruleInfo);
                WriteSubTitleElement_Severity(ruleInfo);
                WriteSubTitleElement_RuleKey(ruleInfo);
                WriteSubTitleElement_Tags(ruleInfo);

                writer.WriteEndElement();
            }

            private void WriteSubTitleElement_IssueType(IRuleInfo ruleInfo)
            {
                var imageInfo = SubTitleImageInfo.IssueTypeImages[ruleInfo.IssueType];
                WriteSubTitleElementWithImage(imageInfo);
            }

            private void WriteSubTitleElement_Severity(IRuleInfo ruleInfo)
            {
                var imageInfo = SubTitleImageInfo.SeverityImages[ruleInfo.DefaultSeverity];
                WriteSubTitleElementWithImage(imageInfo);
            }

            private void WriteSubTitleElementWithImage(SubTitleImageInfo imageInfo)
            {
                writer.WriteStartElement("Span");
                writer.ApplyStyleToElement(StyleResourceNames.SubtitleElement_Span);

                if (imageInfo.ImageResourceName != null)
                {
                    writer.WriteStartElement("InlineUIContainer");
                    writer.WriteStartElement("Image");
                    writer.ApplyStyleToElement(StyleResourceNames.SubtitleElement_Image);
                    writer.WriteAttributeString("Source", $"{{DynamicResource {imageInfo.ImageResourceName}}}");
                    writer.WriteEndElement(); // Image
                    writer.WriteEndElement(); // InlineUIContainer
                }

                writer.WriteString(imageInfo.DisplayText);
                writer.WriteEndElement(); // Span
            }

            private void WriteSubTitleElement_Tags(IRuleInfo ruleInfo)
            {
                if (ruleInfo.Tags.Count == 0)
                {
                    return;
                }

                // TODO: icon
                WriteSubTitleElement("Tags: " + string.Join(" ", ruleInfo.Tags));
            }

            private void WriteSubTitleElement_RuleKey(IRuleInfo ruleInfo)
            {
                WriteSubTitleElement(ruleInfo.FullRuleKey);
            }

            private void WriteSubTitleElement(string text)
            {
                writer.WriteStartElement("Span");
                writer.ApplyStyleToElement(StyleResourceNames.SubtitleElement_Span);
                writer.WriteString(text);
                writer.WriteEndElement();
            }
        }
    }
}
