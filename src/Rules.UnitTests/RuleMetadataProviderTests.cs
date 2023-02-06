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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class RuleMetadataProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RuleMetadataProvider, IRuleMetadataProvider>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void GetRuleHelp_UnknownLanguage_ReturnsMissingHelp()
        {
            var testSubject = CreateTestSubject();

            var actual = testSubject.GetRuleHelp(Language.Unknown, "S100");

            actual.Language.Should().Be(Language.Unknown);
            actual.RuleKey.Should().Be("S100");
            actual.HtmlDescription.Should().Be(Resources.Rules_DescriptionForMissingRule);
        }

        [TestMethod]
        public void GetRuleHelp_UnknownRuleKey_ReturnsMissingHelp()
        {
            var testSubject = CreateTestSubject();

            var actual = testSubject.GetRuleHelp(Language.Cpp, "unknown rule key");

            actual.Language.Should().Be(Language.Cpp);
            actual.RuleKey.Should().Be("unknown rule key");
            actual.HtmlDescription.Should().Be(Resources.Rules_DescriptionForMissingRule);
        }

        [TestMethod]
        public void GetRuleHelp_CheckAllRules()
        {
            // Perf: this checks around 2000 embedded resources currently, but only
            // takes < 0.5 seconds to run locally.

            // Note: this test isn't checking that the correct resources are embedded.
            // That is done by the EmbeddedResourceTests class.

            var testSubject = CreateTestSubject();
            var resourceNames = testSubject.GetType().Assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(".json"));

            Console.WriteLine("Number of embedded rule descriptions: " + resourceNames.Count());

            foreach (var name in resourceNames)
            {
                CheckRule(name);
            }

            void CheckRule(string fullResourceName)
            {
                Console.WriteLine("Checking " + fullResourceName);

                (Language language, string ruleKey) = GetLanguageAndKeyFromResourceName(fullResourceName);
                var expectedDescription = GetEmbeddedRuleDescription(fullResourceName);

                var actual = testSubject.GetRuleHelp(language, ruleKey);

                actual.Language.Should().Be(language);
                actual.RuleKey.Should().Be(ruleKey);
                actual.HtmlDescription.Should().Be(expectedDescription);
                actual.HtmlDescription.Should().NotBeNullOrWhiteSpace();
            }
        }

        private static RuleMetadataProvider CreateTestSubject()
            => new RuleMetadataProvider(new TestLogger(logToConsole: true));

        private static (Language Language, string ruleKey) GetLanguageAndKeyFromResourceName(string fullResourceName)
        {
            // Names are expected to be in the format:
            //   SonarLint.VisualStudio.Rules.Embedded.{language key}.{rule key}
            // e.g. SonarLint.VisualStudio.Rules.Embedded.cpp.S101.json
            var parts = fullResourceName.Split('.');
            var languageKey = parts[parts.Length - 3];
            var ruleKey = parts[parts.Length - 2];

            var language = Language.GetLanguageFromLanguageKey(languageKey);
            return (language, ruleKey);
        }

        private static string GetEmbeddedRuleDescription(string fullResourceName)
        {
            using var reader = new StreamReader(typeof(RuleMetadataProvider).Assembly.GetManifestResourceStream(fullResourceName));
            var data = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<RuleInfo>(data).Description;
        }
    }
}
