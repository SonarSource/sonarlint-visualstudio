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
using System.Linq;
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
        [TestMethod]
        [DataRow("c", 1200)]
        [DataRow("cpp", 500)]
        [DataRow("js", 200)]
        [DataRow("ts", 200)]
        [DataRow("cs", 350)]
        [DataRow("vbnet", 140)]
        public void CheckEmbeddedHelpFiles_ByLanguage(string languageKey, int atLeastXRules)
        {
            // Sanity check that the number of rules for each language is at least in the right ballpark.

            // We don't check an exact number as we don't want to have to update this test every time
            // we update the analyzers.

            // TODO: embed the rule json file, and use that to check for the expected rules
            var resourceNames = typeof(RuleMetadataProvider).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith("SonarLint.VisualStudio.Rules.Help." + languageKey));

            Console.WriteLine($"{languageKey}: number of rules = {resourceNames.Count()}");
            foreach (var resourceName in resourceNames)
            {
                Console.WriteLine(resourceName);
            }

            resourceNames.Should().HaveCountGreaterThan(atLeastXRules);
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
            using var jsonStream = typeof(RuleMetadataProvider).Assembly
                .GetManifestResourceStream($"SonarLint.VisualStudio.Rules.Help.{languageKey}.rules.json" );

            jsonStream.Should().NotBeNull();
        }

    }
}
