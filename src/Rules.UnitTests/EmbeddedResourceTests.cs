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
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    /// <summary>
    /// Tests that the assembly contains the expected embedded resources
    /// </summary>
    [TestClass]
    public class EmbeddedResourceTests
    {
        private const string BaseResourcePath = "SonarLint.VisualStudio.Rules.Embedded.";

        [TestMethod]
        [DataRow("c", 1200)]
        [DataRow("cpp", 500)]
        [DataRow("javascript", 200)]
        [DataRow("typescript", 200)]
        [DataRow("csharpsquid", 350)]
        [DataRow("vbnet", 140)]
        public void CheckEmbeddedDescriptionFiles_ByLanguage(string repoKey, int atLeastXRules)
        {
            // Sanity check that the number of rules for each language is at least in the right ballpark.

            // We don't check an exact number as we don't want to have to update this test every time
            // we update the analyzers.

            // TODO: embed the rule json file, and use that to check for the expected rules
            var resourceNames = typeof(LocalRuleMetadataProvider).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith($"{BaseResourcePath}{repoKey}") && x.EndsWith(".json"));

            Console.WriteLine($"{repoKey}: number of rules = {resourceNames.Count()}");
            foreach (var resourceName in resourceNames)
            {
                Console.WriteLine(resourceName);
            }

            resourceNames.Should().HaveCountGreaterThan(atLeastXRules);
        }

        [TestMethod]
        public void CheckEmbeddedDescriptionFiles_AreParseableAsXml()
        {
            // Performance: this test is loading and parsing nearly 2000 files,
            // but is still only takes a few hundred milliseconds.
            var asm = typeof(LocalRuleMetadataProvider).Assembly;
            var resourceNames = asm.GetManifestResourceNames()
                .Where(x => x.EndsWith(".desc"));

            Console.WriteLine("Checking embedded files. Count = " + resourceNames.Count());
            var failures = resourceNames.Where(x => !ReadResourceAsXml(asm, x))
                .ToArray();

            failures.Should().HaveCount(0);
        }

        private static bool ReadResourceAsXml(Assembly asm, string fullResourceName)
        {
            try
            {
                var readerSettings = new XmlReaderSettings
                {
                    ConformanceLevel = ConformanceLevel.Fragment
                };
                using var stream = new StreamReader(asm.GetManifestResourceStream(fullResourceName));
                using var reader = XmlReader.Create(stream, readerSettings);

                while (reader.Read()) { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + fullResourceName);
                Console.WriteLine("    " + ex.Message);
                return false;
            }
            return true;
        }

        [Ignore] // not yet implemented
        [TestMethod]
        [DataRow("c")]
        [DataRow("cpp")]
        [DataRow("js")]
        [DataRow("ts")]
        [DataRow("cs")]
        [DataRow("vbnet")]
        public void CheckEmbeddedJson_ByLanguage(string languageKey)
        {
            using var jsonStream = typeof(LocalRuleMetadataProvider).Assembly
                .GetManifestResourceStream($"{BaseResourcePath}{languageKey}.rules.json");

            jsonStream.Should().NotBeNull();
        }
    }
}
