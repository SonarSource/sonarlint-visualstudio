﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Text;
using System.Xml;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator
{
    [TestClass]
    public class XamlGeneratorHelperTests
    {
        [TestMethod]
        public void Factory_Create_ReturnsNonNull()
        {
            var testSubject = new XamlGeneratorHelperFactory();

            var xamlGeneratorHelper = testSubject.Create(Mock.Of<XmlWriter>());

            xamlGeneratorHelper.Should().NotBeNull();
        }

        [TestMethod]
        public void WriteDocumentHeaderAndEndDocument_ProduceCorrectStructure()
        {
            var sb = new StringBuilder();
            var xmlWriter = new XamlWriterFactory().Create(sb);
            var ruleInfo = new RuleInfo("cs:123", "<p>Hi</p>", "Hi", RuleIssueSeverity.Critical,
                RuleIssueType.Vulnerability, null, null, null, null);
            IXamlGeneratorHelper testSubject = CreateTestSubject(xmlWriter);

            testSubject.WriteDocumentHeader(ruleInfo);
            xmlWriter.WriteStartElement("Section");
            xmlWriter.WriteEndElement();
            testSubject.EndDocument();

            sb.ToString().Should().BeEquivalentTo(
@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">Hi</Paragraph>
  <Paragraph Style=""{DynamicResource Subtitle_Paragraph}"">
    <Span Style=""{DynamicResource SubtitleElement_Span}"">
      <InlineUIContainer>
        <Image Style=""{DynamicResource SubtitleElement_Image}"" Source=""{DynamicResource vulnerabilityDrawingImage}"" />
      </InlineUIContainer>Vulnerability</Span>
    <Span Style=""{DynamicResource SubtitleElement_Span}"">
      <InlineUIContainer>
        <Image Style=""{DynamicResource SubtitleElement_Image}"" Source=""{DynamicResource criticalDrawingImage}"" />
      </InlineUIContainer>Critical</Span>
    <Span Style=""{DynamicResource SubtitleElement_Span}"">cs:123</Span>
  </Paragraph>
  <Section />
</FlowDocument>".Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }

        [TestMethod]
        public void WriteDocumentHeaderAndEndDocument_ProduceCorrectStructure_NewCCT()
        {
            var sb = new StringBuilder();
            var xmlWriter = new XamlWriterFactory().Create(sb);
            var ruleInfo = new RuleInfo("cs:123", "<p>Hi</p>", "Hi", RuleIssueSeverity.Critical,
                RuleIssueType.Vulnerability, null, CleanCodeAttribute.Formatted, new Dictionary<SoftwareQuality, SoftwareQualitySeverity>
                {
                    { SoftwareQuality.Maintainability, SoftwareQualitySeverity.High},
                    { SoftwareQuality.Security, SoftwareQualitySeverity.Low},
                    { SoftwareQuality.Reliability, SoftwareQualitySeverity.Medium},
                }, null);
            IXamlGeneratorHelper testSubject = CreateTestSubject(xmlWriter);

            testSubject.WriteDocumentHeader(ruleInfo);
            xmlWriter.WriteStartElement("Section");
            xmlWriter.WriteEndElement();
            testSubject.EndDocument();

            sb.ToString().Should().BeEquivalentTo(@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">Hi</Paragraph>
  <BlockUIContainer>
    <WrapPanel>
      <Border Style=""{DynamicResource CleanCodeAttributeBubble}"">
        <TextBlock ToolTip=""Clean Code attributes are characteristics code needs to have to be considered clean"">
          <Span Style=""{DynamicResource CleanCodeSpan}"">
            <Run Style=""{DynamicResource CleanCodeCategory}"">Consistency issue</Run> | Not Formatted</Span>
        </TextBlock>
      </Border>
      <Border Style=""{DynamicResource HighSoftwareQualitySeverityBubble}"">
        <TextBlock ToolTip=""Issues found for this rule will have a High impact on the Maintainability of your software."">
          <Span Style=""{DynamicResource CleanCodeSpan}"">Maintainability</Span>
          <Span Style=""{DynamicResource CleanCodeSpan}"">
            <InlineUIContainer>
              <Image Style=""{DynamicResource CleanCodeSeverityImage}"" Source=""{DynamicResource HighSoftwareQualitySeverity}"" />
            </InlineUIContainer></Span>
        </TextBlock>
      </Border>
      <Border Style=""{DynamicResource LowSoftwareQualitySeverityBubble}"">
        <TextBlock ToolTip=""Issues found for this rule will have a Low impact on the Security of your software."">
          <Span Style=""{DynamicResource CleanCodeSpan}"">Security</Span>
          <Span Style=""{DynamicResource CleanCodeSpan}"">
            <InlineUIContainer>
              <Image Style=""{DynamicResource CleanCodeSeverityImage}"" Source=""{DynamicResource LowSoftwareQualitySeverity}"" />
            </InlineUIContainer></Span>
        </TextBlock>
      </Border>
      <Border Style=""{DynamicResource MediumSoftwareQualitySeverityBubble}"">
        <TextBlock ToolTip=""Issues found for this rule will have a Medium impact on the Reliability of your software."">
          <Span Style=""{DynamicResource CleanCodeSpan}"">Reliability</Span>
          <Span Style=""{DynamicResource CleanCodeSpan}"">
            <InlineUIContainer>
              <Image Style=""{DynamicResource CleanCodeSeverityImage}"" Source=""{DynamicResource MediumSoftwareQualitySeverity}"" />
            </InlineUIContainer></Span>
        </TextBlock>
      </Border>
      <TextBlock>
        <Hyperlink NavigateUri=""https://docs.sonarsource.com/sonarqube-for-ide/visual-studio/concepts/clean-code/introduction"">Learn more about Clean Code</Hyperlink>
      </TextBlock>
    </WrapPanel>
  </BlockUIContainer>
  <Paragraph Style=""{DynamicResource Subtitle_Paragraph}"">
    <Span Style=""{DynamicResource SubtitleElement_Span}"">cs:123</Span>
  </Paragraph>
  <Section />
</FlowDocument>".Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }

        private static IXamlGeneratorHelper CreateTestSubject(XmlWriter xmlWriter)
        {
            return new XamlGeneratorHelperFactory().Create(xmlWriter);
        }
    }
}
