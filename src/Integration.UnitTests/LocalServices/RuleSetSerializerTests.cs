/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.CodeDom.Compiler;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RuleSetSerializerTests
    {
        private TestLogger logger;
        private MockFileSystem fileSystem;
        private TempFileCollection temporaryFiles;
        private RuleSetSerializer testSubject;

        #region Test plumbing

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            logger = new TestLogger();
            fileSystem = new MockFileSystem();
            temporaryFiles = new TempFileCollection(this.TestContext.TestRunDirectory, keepFiles: false);
            testSubject = new RuleSetSerializer(logger, fileSystem);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ((IDisposable)this.temporaryFiles).Dispose();
        }

        #endregion Test plumbing

        #region Tests

        [TestMethod]
        public void Ctor_InvalidArguments()
        {
            Action act = () => new RulesSettingsSerializer(null, Mock.Of<ILogger>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");

            act = () => new RulesSettingsSerializer(Mock.Of<IFileSystem>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void RuleSetSerializer_LoadRuleSet_ArgChecks2(string path)
        {
            Action act = () => testSubject.LoadRuleSet(path);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("path");
        }

        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet()
        {
            // Arrange
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            temporaryFiles.AddFile(ruleSet.FilePath, false);
            ruleSet.WriteToFile(ruleSet.FilePath);
            fileSystem.AddFile(ruleSet.FilePath, new MockFileData(""));

            // Act
            RuleSet loaded = this.testSubject.LoadRuleSet(ruleSet.FilePath);

            // Assert
            loaded.Should().NotBeNull("Expected to load a rule set file");
            RuleSetAssert.AreEqual(ruleSet, loaded, "Loaded unexpected rule set");
        }

        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet_Failures()
        {
            // Arrange
            string existingRuleSet = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            temporaryFiles.AddFile(existingRuleSet, false);

            // Case 1: file not exists
            RuleSet missing = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            missing.Should().BeNull("Expected no ruleset to be loaded when the file is missing");
            AssertRuleSetFileIsLogged();

            // Case 2: file exists, badly formed rule set (invalid xml)
            File.WriteAllText(existingRuleSet, "<xml>");
            fileSystem.AddFile(existingRuleSet, "");

            // Act
            RuleSet loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            loadedBad.Should().BeNull("Expected no ruleset to be loaded when its invalid XML");
            AssertRuleSetFileIsLogged();

            // Case 3: file exists, invalid rule set format (no RuleSet element)
            string xml =
"<?XML version=\"1.0\" encoding=\"utf-8\"?>\n" +
"<Rules AnalyzerId=\"SonarLint.CSharp\" RuleNamespace=\"SonarLint.CSharp\">\n" +
"</Rules>\n";

            File.WriteAllText(existingRuleSet, xml);

            // Act
            loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            loadedBad.Should().BeNull("Expected no ruleset to be loaded when the file in not a valid rule set");
            AssertRuleSetFileIsLogged();

            // Case 4: file exists, invalid rule set data (Default is not a valid action for rule)
            xml =
"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
"<RuleSet Name=\"RS\" ToolsVersion=\"14.0\">\n" +
"<Rules AnalyzerId=\"SonarLint.CSharp\" RuleNamespace=\"SonarLint.CSharp\">\n" +
"       <Rule Id=\"S2360\" Action=\"Default\" />\n" +
"</Rules>\n" +
"</RuleSet>\n";

            File.WriteAllText(existingRuleSet, xml);

            // Act
            loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            loadedBad.Should().BeNull("Expected no ruleset to be loaded when the file in not a valid rule set");
            AssertRuleSetFileIsLogged();

            void AssertRuleSetFileIsLogged()
            {
                logger.AssertPartialOutputStringExists(existingRuleSet);
                logger.OutputStrings.Clear();
            }
        }

        [TestMethod]
        public void RuleSetSerializer_WriteRuleSetFile_InvalidRuleset_Throws()
        {
            Action act = () => testSubject.WriteRuleSetFile(null, @"c:\file.ruleSet");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleSet");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("\t\n")]
        public void RuleSetSerializer_WriteRuleSetFile_InvalidPath_Throws(string path)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");

            Action act = () => testSubject.WriteRuleSetFile(ruleSet, path);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("path");
        }

        [TestMethod]
        public void RuleSetSerializer_WriteRuleSetFile()
        {
            // Arrange
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            string expectedPath = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName + "Other.ruleset");

            // Act
            this.testSubject.WriteRuleSetFile(ruleSet, expectedPath);

            // Assert
            File.Exists(expectedPath).Should().BeTrue("File not exists where expected");
            File.Exists(ruleSet.FilePath).Should().BeFalse("Expected to save only to the specified file path");
            RuleSet loaded = RuleSet.LoadFromFile(expectedPath);
            RuleSetAssert.AreEqual(ruleSet, loaded, "Written unexpected rule set");
        }

        #endregion Tests
    }
}
