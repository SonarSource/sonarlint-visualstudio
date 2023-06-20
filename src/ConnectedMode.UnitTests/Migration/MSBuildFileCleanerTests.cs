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

using System.IO;
using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MSBuildFileCleanerTests
    {
        private static readonly LegacySettings AnyLegacySettings = new LegacySettings("c:\\any\\root\\folder", "csharpruleset", "csharpXML", "vbruleset", "vbXML");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MSBuildFileCleaner, IFileCleaner>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<MSBuildFileCleaner>();

        [TestMethod]
        public void Clean_EmptyDocument_NoErrors_ReturnsNull()
        {
            const string content = "<Project/>";
            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(content, AnyLegacySettings, CancellationToken.None);

            actual.Should().Be(MSBuildFileCleaner.Unchanged);
        }

        [TestMethod]
        public void Clean_AdditionalFileRefsExist_CorrectProjectKey_SettingsAreRemoved()
        {
            var input = LoadEmbeddedTestCase("AdditionalFiles_my_project_key_Input.xml");
            var expected = LoadEmbeddedTestCase("AdditionalFiles_my_project_key_Cleaned.xml");

            var settings = new LegacySettings("any",
                "any",
                ".sonarlint\\my_project_key\\CSharp\\SonarLint.xml",
                "any",
                ".sonarlint\\my_project_key\\vb\\SonarLint.xml");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void Clean_AdditionalFileRefsExist_DifferentProjectKey_SettingsAreNotRemoved()
        {
            // The project key in the settings doesn't match the project key in the file,
            // so the file content should not be changed
            var input = LoadEmbeddedTestCase("AdditionalFiles_my_project_key_Input.xml");

            var settings = new LegacySettings("any",
                "any",
                ".sonarlint\\DIFFERENT_project_key\\CSharp\\SonarLint.xml",
                "any",
                ".sonarlint\\DIFFERENT__project_key\\vb\\SonarLint.xml");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(MSBuildFileCleaner.Unchanged);
        }

        [TestMethod]
        public void Clean_RulesetIncludesExist_CorrectProjectKey_SettingsAreRemoved()
        {
            var input = LoadEmbeddedTestCase("Ruleset_XX-project-key_Input.ruleset.xml");
            var expected = LoadEmbeddedTestCase("Ruleset_XX-project-key_Cleaned.ruleset.xml");

            var settings = new LegacySettings("any",
                ".sonarlint\\XX-project-keycsharp.ruleset",
                "any",
                ".sonarlint\\XX-project-keyvb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void Clean_RulesetIncludessExist_DifferentProjectKey_SettingsAreNotRemoved()
        {
            var input = LoadEmbeddedTestCase("Ruleset_XX-project-key_Input.ruleset.xml");

            var settings = new LegacySettings("any",
                ".sonarlint\\some-other-keycsharp.ruleset",
                "any",
                ".sonarlint\\some-other-keyvb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(MSBuildFileCleaner.Unchanged);
        }

        [TestMethod]
        public void Clean_RulesetPropertiesExist_CorrectProjectKey_SettingsAreRemoved()
        {
            var input = LoadEmbeddedTestCase("RulesetProp-project_key_aaa_Input.vbproj");
            var expected = LoadEmbeddedTestCase("RulesetProp-project_key_aaa_Cleaned.vbproj");

            var settings = new LegacySettings("any",
                ".sonarlint\\project_key_aaacsharp.ruleset",
                "any",
                ".sonarlint\\project_key_aaavb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void Clean_RulesetRefsExist_DifferentProjectKey_SettingsAreNotRemoved()
        {
            var input = LoadEmbeddedTestCase("RulesetProp-project_key_aaa_Input.vbproj");

            var settings = new LegacySettings("any",
                ".sonarlint\\another_project_keycsharp.ruleset",
                "any",
                ".sonarlint\\another_project_keyvb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            actual.Should().Be(MSBuildFileCleaner.Unchanged);
        }

        private static MSBuildFileCleaner CreateTestSubject(ILogger logger = null)
        {
            logger ??= new TestLogger(logToConsole: true);
            return new MSBuildFileCleaner(logger);
        }

        private static string LoadEmbeddedTestCase(string testResourceName)
        {
            var resourcePath = "SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.FileCleanerTestCases." + testResourceName;
            using var stream = new StreamReader(typeof(MSBuildFileCleanerTests).Assembly.GetManifestResourceStream(resourcePath));

            return stream.ReadToEnd();
        }
    }
}
