/*
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

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.Layout.Logical;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.Rules;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator;

[TestClass]
public class RuleXamlBuilderSmokeTest
{
    // private static readonly Assembly ResourceAssembly = typeof(LocalRuleMetadataProvider).Assembly;
    //
    // [TestMethod]
    // public void Create_CheckAllEmbedded()
    // {
    //     // Performance: this test is loading nearly 2000 files and creating
    //     // XAML document for them, but it still only takes a around 3 seconds
    //     // to run.
    //     var resourceNames = ResourceAssembly.GetManifestResourceNames()
    //         .Where(x => x.EndsWith(".json"));
    //
    //     // Sanity check - should have checked at least 1500 rules
    //     resourceNames.Count().Should().BeGreaterThan(1500);
    //
    //     Console.WriteLine("Checking xaml creation. Count = " + resourceNames.Count());
    //
    //     string[] failures;
    //     using (new AssertIgnoreScope()) // the product code can assert if it encounters an unrecognised tag
    //     {
    //         failures = resourceNames.Where(x => !ProcessResource(x))
    //             .ToArray();
    //     }
    //
    //     failures.Should().BeEquivalentTo(new[]
    //     {
    //         // introduced in sonar-cpp 6.48
    //         "SonarLint.VisualStudio.Rules.Embedded.cpp.S1232.json",
    //         // some issue with diff highlighting
    //         "SonarLint.VisualStudio.Rules.Embedded.csharpsquid.S6640.json",
    //     });
    // }
    //
    // private static bool ProcessResource(string fullResourceName)
    // {
    //     var xamlWriterFactory = new XamlWriterFactory();
    //     var ruleHelpXamlTranslatorFactory = new RuleHelpXamlTranslatorFactory(xamlWriterFactory, new DiffTranslator(xamlWriterFactory));
    //     var xamlGeneratorHelperFactory = new XamlGeneratorHelperFactory(ruleHelpXamlTranslatorFactory);
    //     var ruleInfoTranslator = new RuleInfoTranslator(ruleHelpXamlTranslatorFactory, new TestLogger());
    //     var staticXamlStorage = new StaticXamlStorage(ruleHelpXamlTranslatorFactory);
    //
    //     var simpleXamlBuilder = new SimpleRuleHelpXamlBuilder(ruleHelpXamlTranslatorFactory, xamlGeneratorHelperFactory, xamlWriterFactory);
    //     var richXamlBuilder = new RichRuleHelpXamlBuilder(ruleInfoTranslator, xamlGeneratorHelperFactory, staticXamlStorage, xamlWriterFactory);
    //
    //     try
    //     {
    //         bool res = false;
    //
    //         var data = ReadResource(fullResourceName);
    //         var jsonRuleInfo = LocalRuleMetadataProvider.RuleInfoJsonDeserializer.Deserialize(data);
    //
    //         if (!string.IsNullOrWhiteSpace(jsonRuleInfo.Description))
    //         {
    //             Res(simpleXamlBuilder.Create(jsonRuleInfo));
    //             res = true;
    //         }
    //
    //         if (jsonRuleInfo.DescriptionSections.Any())
    //         {
    //             Res(richXamlBuilder.Create(jsonRuleInfo, null));
    //             res = true;
    //         }
    //
    //         return res; // simple || rich should be true
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine("Failed: " + fullResourceName);
    //         Console.WriteLine("    " + ex.Message);
    //         return false;
    //     }
    // }
    //
    // private static void Res(FlowDocument doc)
    // {
    //     // Quick sanity check that something was produced
    //     // Note: this is a quick way of getting the size of the document. Serializing the doc to a string
    //     // and checking the length takes much longer (around 25 seconds)
    //     var docLength = doc.ContentStart.DocumentStart.GetOffsetToPosition(doc.ContentEnd.DocumentEnd);
    //     docLength.Should().BeGreaterThan(30);
    // }
    //
    // private static string ReadResource(string fullResourceName)
    // {
    //     using var stream = new StreamReader(ResourceAssembly.GetManifestResourceStream(fullResourceName));
    //     return stream.ReadToEnd();
    // }
}
