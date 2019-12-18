/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.IO;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class RulesMetadataCacheTest
    {
        // Rule data for files in CFamily\TestResources\RulesMetadataCache
        private const int Active_C_Rules = 3;
        private const int Inactive_C_Rules = 3;

        private const int Active_CPP_Rules = 4;
        private const int Inactive_CPP_Rules = 2;

        private RulesMetadataCache rulesMetadataCache = CreateTestSubject();

        [TestMethod]
        public void Settings_LanguageKey()
        {
            rulesMetadataCache.GetSettings("c").LanguageKey.Should().Be("c");
            rulesMetadataCache.GetSettings("cpp").LanguageKey.Should().Be("cpp");

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetSettings("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Rules()
        {
            rulesMetadataCache.GetSettings("c").AllPartialRuleKeys.Should().HaveCount(Active_C_Rules + Inactive_C_Rules);
            rulesMetadataCache.GetSettings("cpp").AllPartialRuleKeys.Should().HaveCount(Active_CPP_Rules + Inactive_CPP_Rules);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetSettings("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Active_Rules()
        {
            rulesMetadataCache.GetSettings("c").ActivePartialRuleKeys.Should().HaveCount(Active_C_Rules);
            rulesMetadataCache.GetSettings("cpp").ActivePartialRuleKeys.Should().HaveCount(Active_CPP_Rules);

            // We don't currently support ObjC rules in VS
            rulesMetadataCache.GetSettings("objc").Should().BeNull();
        }

        [TestMethod]
        public void Read_Rules_Params()
        {
            rulesMetadataCache.GetSettings("cpp").RulesParameters.TryGetValue("All_ActiveWithParams_1", out var parameters);
            parameters.Should()
                .Contain(new System.Collections.Generic.KeyValuePair<string, string>("maximumClassComplexityThreshold", "80"));
        }

        [TestMethod]
        public void Read_Rules_Metadata()
        {
            rulesMetadataCache.GetSettings("cpp").RulesMetadata.TryGetValue("All_ActiveWithParams_1", out var metadata);
            using (new AssertionScope())
            {
                metadata.Type.Should().Be(IssueType.CodeSmell);
                metadata.DefaultSeverity.Should().Be(IssueSeverity.Critical);
            }
        }

        private static RulesMetadataCache CreateTestSubject()
        {
            var resourcesPath = Path.Combine(
                Path.GetDirectoryName(typeof(RulesMetadataCache).Assembly.Location),
                "CFamily", "TestResources", "RulesMetadataCache");
            Directory.Exists(resourcesPath).Should().BeTrue($"Test setup error: expected test resources directory does not exist: {resourcesPath}");

            var testSubject = new RulesMetadataCache(resourcesPath);
            return testSubject;
        }

    }
}
