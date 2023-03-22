﻿/*
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

using System.Collections.Generic;
using System.Text;
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class XamlGeneratorHelperTests
    {
        [TestMethod]
        public void Factory_Create_ReturnsNonNull()
        {
            var testSubject = new XamlGeneratorHelperFactory(Mock.Of<IRuleHelpXamlTranslator>());

            var xamlGeneratorHelper = testSubject.Create(Mock.Of<XmlWriter>());

            xamlGeneratorHelper.Should().NotBeNull();
        }

        [TestMethod]
        public void WriteDocumentHeaderAndEndDocument_ExtendedDescription_ProduceCorrectStructure()
        {
            var sb = new StringBuilder();
            var xmlWriter = RuleHelpXamlTranslator.CreateXmlWriter(sb);
            var ruleInfo = new RuleInfo("cs", "cs:123", "<p>Hi</p>", "Hi", RuleIssueSeverity.Critical,
                RuleIssueType.Vulnerability, true, new List<string>(), new List<IDescriptionSection>(),
                new List<string>(), "<p>fix this pls</p>");

            var testSubject = (new XamlGeneratorHelperFactory(new RuleHelpXamlTranslator())).Create(xmlWriter);

            testSubject.WriteDocumentHeader(ruleInfo);
            xmlWriter.WriteStartElement("LineBreak");
            xmlWriter.WriteEndElement();
            testSubject.EndDocument();

            sb.ToString().Should().BeEquivalentTo(
@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">Hi</Paragraph>
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">
    <Span Style=""{DynamicResource SubtitleElement_Span}"">
      <InlineUIContainer>
        <Image Style=""{DynamicResource SubtitleElement_Image}"" Source=""{DynamicResource vulnerabilityDrawingImage}"" />
      </InlineUIContainer>Vulnerability</Span>
    <Span Style=""{DynamicResource SubtitleElement_Span}"">
      <InlineUIContainer>
        <Image Style=""{DynamicResource SubtitleElement_Image}"" Source=""{DynamicResource criticalDrawingImage}"" />
      </InlineUIContainer>Critical</Span>
    <Span Style=""{DynamicResource SubtitleElement_Span}"">cs:123</Span>
  </Paragraph><Paragraph>fix this pls</Paragraph><LineBreak /></FlowDocument>".Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }

        [TestMethod]
        public void WriteDocumentHeaderAndEndDocument_ProduceCorrectStructure()
        {
            var sb = new StringBuilder();
            var xmlWriter = RuleHelpXamlTranslator.CreateXmlWriter(sb);
            var ruleInfo = new RuleInfo("cs", "cs:123", "<p>Hi</p>", "Hi", RuleIssueSeverity.Critical,
                RuleIssueType.Vulnerability, true, new List<string>(), new List<IDescriptionSection>(),
                new List<string>(), null);

            var testSubject = (new XamlGeneratorHelperFactory(new RuleHelpXamlTranslator())).Create(xmlWriter);

            testSubject.WriteDocumentHeader(ruleInfo);
            xmlWriter.WriteStartElement("Section");
            xmlWriter.WriteEndElement();
            testSubject.EndDocument();

            sb.ToString().Should().BeEquivalentTo(
@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">Hi</Paragraph>
  <Paragraph Style=""{DynamicResource Title_Paragraph}"">
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
    }
}
