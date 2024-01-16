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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Rules.UnitTests
{
    [TestClass]
    public class LocalRuleMetadataProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<LocalRuleMetadataProvider, ILocalRuleMetadataProvider>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void GetRuleHelp_UnknownRepo_ReturnsNullp()
        {
            var ruleId = new SonarCompositeRuleId("unknown repo key", "S100");

            var testSubject = CreateTestSubject();

            var actual = testSubject.GetRuleInfo(ruleId);

            actual.Should().BeNull();
        }

        [TestMethod]
        public void GetRuleHelp_UnknownRuleKey_ReturnsMissingHelp()
        {
            var ruleId = new SonarCompositeRuleId("cpp", "unknown rule key");

            var testSubject = CreateTestSubject();

            var actual = testSubject.GetRuleInfo(ruleId);

            actual.Should().BeNull();
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

                SonarCompositeRuleId ruleId = GetCompositeRuleIdFromResourceName(fullResourceName);
                var expectedRuleInfo = GetEmbeddedRuleInfo(fullResourceName);

                var actual = testSubject.GetRuleInfo(ruleId);

                actual.FullRuleKey.Should().Be(ruleId.ToString());
                actual.Description.Should().Be(expectedRuleInfo.Description);
                actual.DescriptionSections.Should().BeEquivalentTo(expectedRuleInfo.DescriptionSections);
                actual.DescriptionSections.All(section => !string.IsNullOrWhiteSpace(section.HtmlContent)).Should().BeTrue();

                (!string.IsNullOrWhiteSpace(actual.Description) || actual.DescriptionSections.Count > 0)
                    .Should()
                    .BeTrue();
            }
        }

        private static LocalRuleMetadataProvider CreateTestSubject()
            => new LocalRuleMetadataProvider(new TestLogger(logToConsole: true));

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

        private static IRuleInfo GetEmbeddedRuleInfo(string fullResourceName)
        {
            using var reader = new StreamReader(typeof(LocalRuleMetadataProvider).Assembly.GetManifestResourceStream(fullResourceName));
            var data = reader.ReadToEnd();
            return LocalRuleMetadataProvider.RuleInfoJsonDeserializer.Deserialize(data);
        }
    }
}
