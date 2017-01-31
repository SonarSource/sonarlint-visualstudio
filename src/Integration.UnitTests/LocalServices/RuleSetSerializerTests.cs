/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

using Xunit;
using System;
using System.CodeDom.Compiler;
using System.IO;
using FluentAssertions;
using SonarLint.VisualStudio.Integration.UnitTests.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class RuleSetSerializerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableFileSystem fileSystem;
        private TempFileCollection temporaryFiles;
        private RuleSetSerializer testSubject;

        public RuleSetSerializerTests()
        {
            this.temporaryFiles = new TempFileCollection(TestHelper.GetDeploymentDirectory(), keepFiles: false);

            this.serviceProvider = new ConfigurableServiceProvider();

            this.fileSystem = new ConfigurableFileSystem();
            this.serviceProvider.RegisterService(typeof(IFileSystem), this.fileSystem);

            this.testSubject = new RuleSetSerializer(this.serviceProvider);
        }

        #region Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t\n")]
        public void LoadRuleSet_WithNullOrEmptyOrWhiteSpaceRuleSetPath_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => this.testSubject.LoadRuleSet(value);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void RuleSetSerializer_LoadRuleSet()
        {
            // Arrange
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(TestHelper.GetDeploymentDirectory(), "" + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            ruleSet.WriteToFile(ruleSet.FilePath);
            this.fileSystem.RegisterFile(ruleSet.FilePath);

            // Act
            RuleSet loaded = this.testSubject.LoadRuleSet(ruleSet.FilePath);

            // Assert
            loaded.Should().NotBeNull("Expected to load a rule set file");
            RuleSetAssert.AreEqual(ruleSet, loaded, "Loaded unexpected rule set");
        }

        [Fact]
        public void RuleSetSerializer_LoadRuleSet_Failures()
        {
            // Arrange
            string existingRuleSet = Path.Combine(TestHelper.GetDeploymentDirectory(), "" + ".ruleset");
            this.temporaryFiles.AddFile(existingRuleSet, false);

            // Case 1: file not exists
            RuleSet missing = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            missing.Should().BeNull("Expected no ruleset to be loaded when the file is missing");

            // Case 2: file exists, badly formed rule set (invalid xml)
            File.WriteAllText(existingRuleSet, "<xml>");
            this.fileSystem.RegisterFile(existingRuleSet);

            // Act
            RuleSet loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Assert
            loadedBad.Should().BeNull("Expected no ruleset to be loaded when its invalid XML");

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
        }

        [Fact]
        public void WriteRuleSetFile_WithNullRuleSetPath_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => this.testSubject.WriteRuleSetFile(null, @"c:\file.ruleSet");

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t\n")]
        public void WriteRuleSetFile_WithNullOrEmpty_ThrowsArgumentNullException(string value)
        {
            // Arrange
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(TestHelper.GetDeploymentDirectory(), "" + ".ruleset");

            // Act
            Action act = () => this.testSubject.WriteRuleSetFile(ruleSet, value);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void RuleSetSerializer_WriteRuleSetFile()
        {
            // Arrange
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(TestHelper.GetDeploymentDirectory(), "" + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            string expectedPath = TestHelper.GetDeploymentDirectory() + "Other.ruleset";

            // Act
            this.testSubject.WriteRuleSetFile(ruleSet, expectedPath);

            // Assert
            File.Exists(expectedPath).Should().BeTrue("File not exists where expected");
            File.Exists(ruleSet.FilePath)
                .Should().BeFalse("Expected to save only to the specified file path");
            RuleSet loaded = RuleSet.LoadFromFile(expectedPath);
            RuleSetAssert.AreEqual(ruleSet, loaded, "Written unexpected rule set");
        }
        #endregion
    }
}
