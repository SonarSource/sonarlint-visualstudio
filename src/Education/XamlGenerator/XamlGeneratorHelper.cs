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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml;
using SonarLint.VisualStudio.Core.Analysis;
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

    [Export(typeof(IXamlGeneratorHelperFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class XamlGeneratorHelperFactory : IXamlGeneratorHelperFactory
    {
        private readonly IRuleHelpXamlTranslatorFactory ruleHelpXamlTranslatorFactory;

        [ImportingConstructor]
        public XamlGeneratorHelperFactory(IRuleHelpXamlTranslatorFactory ruleHelpXamlTranslatorFactory)
        {
            this.ruleHelpXamlTranslatorFactory = ruleHelpXamlTranslatorFactory;
        }

        public IXamlGeneratorHelper Create(XmlWriter writer)
        {
            return new XamlGeneratorHelper(writer, ruleHelpXamlTranslatorFactory.Create());
        }

        private sealed class XamlGeneratorHelper : IXamlGeneratorHelper
        {
            private static readonly Dictionary<SoftwareQualitySeverity, StyleResourceNames>
                SoftwareQualityBubblesStyles =
                    new Dictionary<SoftwareQualitySeverity, StyleResourceNames>
                    {
                        { SoftwareQualitySeverity.High, StyleResourceNames.HighSoftwareQualitySeverityBubble },
                        { SoftwareQualitySeverity.Medium, StyleResourceNames.MediumSoftwareQualitySeverityBubble },
                        { SoftwareQualitySeverity.Low, StyleResourceNames.LowSoftwareQualitySeverityBubble },
                    };

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
                var isNewCct = ruleInfo.CleanCodeAttribute.HasValue;

                if (isNewCct)
                {
                    WriteCleanCodeHeader(ruleInfo);
                }

                writer.WriteStartElement("Paragraph");
                writer.ApplyStyleToElement(StyleResourceNames.Subtitle_Paragraph);

                if (!isNewCct)
                {
                    WriteSubTitleElement_IssueType(ruleInfo);
                    WriteSubTitleElement_Severity(ruleInfo);
                }

                WriteSubTitleElement_RuleKey(ruleInfo);
                WriteSubTitleElement_Tags(ruleInfo);

                writer.WriteEndElement();
            }

            #region Subtitle

            #region New Clean Code Taxonomy

            private void WriteCleanCodeHeader(IRuleInfo ruleInfo)
            {
                writer.WriteStartElement("BlockUIContainer");
                writer.WriteStartElement("WrapPanel");
                WriteCleanCodeHeader_CleanCodeAttribute(ruleInfo);
                WriteCleanCodeHeader_SoftwareQualities(ruleInfo);
                WriteCleanCodeHeader_Url();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            private void WriteCleanCodeHeader_Url()
            {
                writer.WriteStartElement("TextBlock");
                WriteHyperLink("https://docs.sonarsource.com/sonarlint/visual-studio/concepts/clean-code", Resources.CleanCodeHyperLink);
                writer.WriteEndElement();
            }

            private void WriteCleanCodeHeader_CleanCodeAttribute(IRuleInfo ruleInfo)
            {
                var cleanCodeCategory = CleanCodeAttributeToCategoryMapping.Map[ruleInfo.CleanCodeAttribute.Value];
                var cleanCodeAttribute = ruleInfo.CleanCodeAttribute;

                WriteBubble(StyleResourceNames.CleanCodeAttributeBubble,
                    () =>
                    {
                        writer.WriteAttributeString("ToolTip", Resources.CCATooltip);
                        WriteStylizedSpan(StyleResourceNames.CleanCodeSpan,
                            () =>
                            {
                                WriteCleanCodeCategory(cleanCodeCategory);
                                writer.WriteString($" | Not {cleanCodeAttribute}");
                            });
                    });
            }

            private void WriteCleanCodeCategory(CleanCodeCategory cleanCodeCategory)
            {
                writer.WriteStartElement("Run");
                writer.ApplyStyleToElement(StyleResourceNames.CleanCodeCategory);
                writer.WriteString($"{cleanCodeCategory} issue");
                writer.WriteEndElement();
            }

            private void WriteCleanCodeHeader_SoftwareQualities(IRuleInfo ruleInfo)
            {
                foreach (var softwareQualityAndSeverity in ruleInfo.DefaultImpacts)
                {
                    var imageInfo = SubTitleImageInfo.SoftwareQualitySeveritiesImages[softwareQualityAndSeverity.Value];

                    var softwareQuality = softwareQualityAndSeverity.Key.ToString();
                    var softwareQualitySeverity = softwareQualityAndSeverity.Value.ToString();

                    WriteBubble(SoftwareQualityBubblesStyles[softwareQualityAndSeverity.Value],
                        () =>
                        {
                            writer.WriteAttributeString("ToolTip", string.Format(Resources.SQTooltip, softwareQualitySeverity, softwareQuality));
                            WriteSubTitleElement(softwareQuality, true);
                            WriteSubTitleElementWithImage(imageInfo, true);
                        });
                }
            }

            private void WriteBubble(StyleResourceNames borderStyle, Action writeContent)
            {
                writer.WriteStartElement("Border");
                writer.ApplyStyleToElement(borderStyle);
                // note: content is wrapped in a text block so the icon and the text wrap together and have a common background
                writer.WriteStartElement("TextBlock");
                writeContent();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            #endregion

            #region Old severity

            private void WriteSubTitleElement_IssueType(IRuleInfo ruleInfo)
            {
                var imageInfo = SubTitleImageInfo.IssueTypeImages[ruleInfo.IssueType];
                WriteSubTitleElementWithImage(imageInfo);
            }

            private void WriteSubTitleElement_Severity(IRuleInfo ruleInfo)
            {
                var imageInfo = SubTitleImageInfo.SeverityImages[ruleInfo.Severity];
                WriteSubTitleElementWithImage(imageInfo);
            }

            #endregion

            #region Common

            private void WriteSubTitleElementWithImage(SubTitleImageInfo imageInfo, bool useCctStyle = false) =>
                WriteStylizedSpan(
                    useCctStyle ? StyleResourceNames.CleanCodeSpan : StyleResourceNames.SubtitleElement_Span,
                    () =>
                    {
                        if (imageInfo.ImageResourceName != null)
                        {
                            WriteImage(imageInfo.ImageResourceName, useCctStyle);
                        }

                        writer.WriteString(imageInfo.DisplayText);
                    });

            private void WriteImage(string imageId, bool useCctStyle)
            {
                writer.WriteStartElement("InlineUIContainer");
                writer.WriteStartElement("Image");
                writer.ApplyStyleToElement(useCctStyle
                    ? StyleResourceNames.CleanCodeSeverityImage
                    : StyleResourceNames.SubtitleElement_Image);
                writer.WriteAttributeString("Source", $"{{DynamicResource {imageId}}}");
                writer.WriteEndElement(); // Image
                writer.WriteEndElement(); // InlineUIContainer
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

            private void WriteSubTitleElement(string text, bool useCctStyle = false)
            {
                WriteStylizedSpan(useCctStyle
                        ? StyleResourceNames.CleanCodeSpan
                        : StyleResourceNames.SubtitleElement_Span,
                    () => writer.WriteString(text));
            }

            private void WriteStylizedSpan(StyleResourceNames style, Action writeContent)
            {
                writer.WriteStartElement("Span");
                writer.ApplyStyleToElement(style);
                writeContent();
                writer.WriteEndElement();
            }

            private void WriteHyperLink(string url, string text = null)
            {
                text = text ?? url;

                writer.WriteStartElement("Hyperlink");
                writer.WriteAttributeString("NavigateUri", url);
                writer.WriteString(text);
                writer.WriteEndElement();
            }

            #endregion

            #endregion
        }
    }
}
