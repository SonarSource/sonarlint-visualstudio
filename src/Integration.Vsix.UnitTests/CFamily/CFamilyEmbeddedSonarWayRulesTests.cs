/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Linq;
using System.Net.Http;
using System.Xml;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyEmbeddedSonarWayRulesTests
    {
        // Sanity checks that the rules metata for the CFamily plugin is present and can be loaded
        private static int Active_C_Rules;
        private static int Active_CPP_Rules;

        private readonly CFamilySonarWayRulesConfigProvider rulesMetadataCache = new CFamilySonarWayRulesConfigProvider(CFamilyShared.CFamilyFilesDirectory);

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Active_C_Rules = LoadRulesCount("c");
            Active_CPP_Rules = LoadRulesCount("cpp");
        }

        private static int LoadRulesCount(string language)
        {
            using var client = new HttpClient();
            var uri = $"https://sonarcloud.io/api/qualityprofiles/backup?qualityProfile=Sonar%20way&language={language}&organization=microsoft";
            var rulesXml = client.GetStringAsync(uri).Result;

            var xmldoc = new XmlDocument();
            xmldoc.LoadXml(rulesXml);
            return xmldoc.GetElementsByTagName("rule").Count;
        }

        [TestMethod]
        public void Read_Rules()
        {
            rulesMetadataCache.GetRulesConfiguration("c").AllPartialRuleKeys.Should().HaveCountGreaterThan(Active_C_Rules);
            rulesMetadataCache.GetRulesConfiguration("cpp").AllPartialRuleKeys.Should().HaveCountGreaterThan(Active_CPP_Rules);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetRulesConfiguration("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Active_Rules()
        {
            const int versionDifferenceBuffer = 10;
            rulesMetadataCache.GetRulesConfiguration("c").ActivePartialRuleKeys.Count().Should().BeInRange(Active_C_Rules- versionDifferenceBuffer, Active_C_Rules+ versionDifferenceBuffer);
            rulesMetadataCache.GetRulesConfiguration("cpp").ActivePartialRuleKeys.Count().Should().BeInRange(Active_CPP_Rules- versionDifferenceBuffer, Active_CPP_Rules+ versionDifferenceBuffer);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetRulesConfiguration("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Rules_Params()
        {
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesParameters.TryGetValue("ClassComplexity", out var parameters);
            parameters.Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("maximumClassComplexityThreshold", "80"));
        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            rulesMetadataCache.GetRulesConfiguration("cpp").RulesMetadata.TryGetValue("ClassComplexity", out var metadata);
            using (new AssertionScope())
            {
                metadata.Type.Should().Be(IssueType.CodeSmell);
                metadata.DefaultSeverity.Should().Be(IssueSeverity.Critical);
            }
        }

        [TestMethod]
        [DataRow("S5536", "c")]
        [DataRow("S5536", "cpp")]
        public void CheckProjectLevelRule_IsDisabledByDefault(string ruleKey, string languageKey)
        {
            rulesMetadataCache.GetRulesConfiguration(languageKey).AllPartialRuleKeys.Contains(ruleKey).Should().BeTrue();
            rulesMetadataCache.GetRulesConfiguration(languageKey).ActivePartialRuleKeys.Contains(ruleKey).Should().BeFalse();
        }
    }
}
