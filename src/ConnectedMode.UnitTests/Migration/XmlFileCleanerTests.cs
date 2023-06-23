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
using System.Threading;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    // NOTE: in the event of a test failure the actual and expected results are
    // written to files and added to the Test Results i.e. they should be
    // accessible at the bottom of the test results pane (and automatically
    // captured by the Azure DevOps CI build).

    [TestClass]
    public class XmlFileCleanerTests
    {
        private static readonly LegacySettings AnyLegacySettings = new LegacySettings("c:\\any\\root\\folder", "csharpruleset", "csharpXML", "vbruleset", "vbXML");
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<XmlFileCleaner, IFileCleaner>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<XmlFileCleaner>();

        [TestMethod]
        public void Clean_EmptyDocument_NoErrors_ReturnsNull()
        {
            const string content = "<Project/>";
            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(content, AnyLegacySettings, CancellationToken.None);

            CheckIsUnchanged(actual);
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
            CheckAreSame(actual, expected);
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
            CheckIsUnchanged(actual);
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
            CheckAreSame(actual, expected);
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
            CheckIsUnchanged(actual);
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
            CheckAreSame(actual, expected);
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
            CheckIsUnchanged(actual);
        }

        [TestMethod]
        public void Clean_None_CorrectProjectKey_SettingsAreRemoved()
        {
            var input = LoadEmbeddedTestCase("AdditionalFiles_XprojectkeyY_NoneItemGroup_Input.xml");
            var expected = LoadEmbeddedTestCase("AdditionalFiles_XprojectkeyY_NoneItemGroup_Cleaned.xml");

            var settings = new LegacySettings("any",
                ".sonarlint\\XprojectkeyYcsharp.ruleset",
                "any",
                ".sonarlint\\XprojectkeyYvb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            CheckAreSame(actual, expected);
        }

        [TestMethod]
        public void Clean_None_DifferentProjectKey_SettingsAreNotRemoved()
        {
            var input = LoadEmbeddedTestCase("AdditionalFiles_XprojectkeyY_NoneItemGroup_Input.xml");
            var expected = LoadEmbeddedTestCase("AdditionalFiles_XprojectkeyY_NoneItemGroup_Cleaned.xml");

            var settings = new LegacySettings("any",
                ".sonarlint\\XXXprojectkeyYYYcsharp.ruleset",
                "any",
                ".sonarlint\\XXXprojectkeyYYYvb.ruleset",
                "any");

            var testSubject = CreateTestSubject();

            var actual = testSubject.Clean(input, settings, CancellationToken.None);
            CheckIsUnchanged(actual);
        }

        private static XmlFileCleaner CreateTestSubject(ILogger logger = null)
        {
            logger ??= new TestLogger(logToConsole: true);
            return new XmlFileCleaner(logger);
        }

        private static string LoadEmbeddedTestCase(string testResourceName)
        {
            var resourcePath = "SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.FileCleanerTestCases." + testResourceName;
            using var stream = new StreamReader(typeof(XmlFileCleanerTests).Assembly.GetManifestResourceStream(resourcePath));

            return stream.ReadToEnd();
        }

        private void CheckAreSame(string actual, string expected)
        {
            // Only dump the files to disk if the test will fail so
            // we don't clutter it up
            if (actual != expected)
            { 
                WriteResultFile("actual.txt", actual);
                WriteResultFile("expected.txt", expected);
            }
            actual.Should().Be(expected);
        }

        private void CheckIsUnchanged(string actual)
        {
            // Only dump the files to disk if the test will fail so
            // we don't clutter it up
            if (actual != XmlFileCleaner.Unchanged)
            {
                WriteResultFile("actual.txt", actual);
            }
            actual.Should().Be(XmlFileCleaner.Unchanged);
        }

        private void WriteResultFile(string fileName, string content)
        {
            var testDir = EnsureTestSpecificDirectoryExists();
            string fullFilePath = Path.Combine(testDir, fileName);
            Console.WriteLine("Writing results file: " + fullFilePath);

            // If the result is null it means the file wasn't changed
            File.WriteAllText(fullFilePath, content ?? "{null i.e. the input was not changed}");
            TestContext.AddResultFile(fullFilePath);
        }

        private string EnsureTestSpecificDirectoryExists()
        {
            var testDir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(testDir);
            return testDir;
        }
    }
}
