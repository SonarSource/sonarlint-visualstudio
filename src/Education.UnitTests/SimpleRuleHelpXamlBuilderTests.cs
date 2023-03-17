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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Rules;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class SimpleRuleHelpXamlBuilderTests
    {
        private static readonly Assembly ResourceAssembly = typeof(LocalRuleMetadataProvider).Assembly;

        [TestMethod]
        public void Create_FormsCorrectStructure()
        {
            var callSequence = new MockSequence();
            var description = "<p>Hi</p>";
            var ruleHelpXamlTranslatorMock = new Mock<IRuleHelpXamlTranslator>(MockBehavior.Strict);
            var xamlGeneratorHelperFactoryMock = new Mock<IXamlGeneratorHelperFactory>(MockBehavior.Strict);
            var xamlGeneratorHelperMock = new Mock<IXamlGeneratorHelper>(MockBehavior.Strict);
            var ruleInfoMock = new Mock<IRuleInfo>(MockBehavior.Strict);
            XmlWriter writer = null;
            
            xamlGeneratorHelperFactoryMock
                .InSequence(callSequence)
                .Setup(x => x.Create(It.IsAny<XmlWriter>()))
                .Callback<XmlWriter>(w => writer = w)
                .Returns(xamlGeneratorHelperMock.Object);
            xamlGeneratorHelperMock
                .InSequence(callSequence)
                .Setup(x => x.WriteDocumentHeader(ruleInfoMock.Object))
                .Callback(() => { writer.WriteStartElement("FlowDocument", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"); });
            ruleInfoMock
                .InSequence(callSequence)
                .SetupGet(x => x.Description)
                .Returns(description);
            ruleHelpXamlTranslatorMock
                .InSequence(callSequence)
                .Setup(x => x.TranslateHtmlToXaml(description))
                .Returns("<Paragraph>Hi</Paragraph>");
            xamlGeneratorHelperMock
                .InSequence(callSequence)
                .Setup(x => x.EndDocument())
                .Callback(() =>
                {
                    writer.WriteFullEndElement();
                    writer.Close();
                });

            var simpleRuleHelpXamlBuilder = new SimpleRuleHelpXamlBuilder(ruleHelpXamlTranslatorMock.Object, xamlGeneratorHelperFactoryMock.Object);

            var flowDocument = simpleRuleHelpXamlBuilder.Create(ruleInfoMock.Object);

            flowDocument.Blocks.Single().Should().BeOfType<Paragraph>().Which.Inlines.Single().Should().BeOfType<Run>().Which.Text.Should().Be("Hi");
        }

        [TestMethod]
        public void Create_CheckAllEmbedded()
        {            
            // Performance: this test is loading nearly 2000 files and creating
            // XAML document for them, but it still only takes a around 3 seconds
            // to run.
            var resourceNames = ResourceAssembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(".json"));

            // Sanity check - should have checked at least 1500 rules
            resourceNames.Count().Should().BeGreaterThan(1500);

            Console.WriteLine("Checking xaml creation. Count = " + resourceNames.Count());
            var failures = resourceNames.Where(x => !ProcessResource(x))
                .ToArray();

            failures.Should().HaveCount(0);
        }

        private static bool ProcessResource(string fullResourceName)
        {
            var testSubject = new SimpleRuleHelpXamlBuilder(new RuleHelpXamlTranslator(), new XamlGeneratorHelperFactory(new RuleHelpXamlTranslator()));

            try
            {
                var data = ReadResource(fullResourceName);
                var jsonRuleInfo = JsonConvert.DeserializeObject<RuleInfo>(data);

                var doc = testSubject.Create(jsonRuleInfo);

                // Quick sanity check that something was produced
                // Note: this is a quick way of getting the size of the document. Serializing the doc to a string
                // and checking the length takes much longer (around 25 seconds)
                var docLength = doc.ContentStart.DocumentStart.GetOffsetToPosition(doc.ContentEnd.DocumentEnd);
                Console.WriteLine($"{jsonRuleInfo.FullRuleKey}: size = {docLength}");
                docLength.Should().BeGreaterThan(30);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + fullResourceName);
                Console.WriteLine("    " + ex.Message);
                return false;
            }
        }

        private static string ReadResource(string fullResourceName)
        {
            using var stream = new StreamReader(ResourceAssembly.GetManifestResourceStream(fullResourceName));
            return stream.ReadToEnd();
        }

        private static SonarCompositeRuleId GetCompositeRuleIdFromResourceName(string fullResourceName)
        {
            // Names are expected to be in the format:
            //   SonarLint.VisualStudio.Rules.Embedded.{repo key}.{rule key}
            // e.g. SonarLint.VisualStudio.Rules.Embedded.cpp.S101.json
            var parts = fullResourceName.Split('.');
            var repoKey = parts[parts.Length - 3];
            var ruleKey = parts[parts.Length - 2];

            return new SonarCompositeRuleId(repoKey, ruleKey);
        }
    }
}
