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

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.IO;
using System.Linq;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{

    public class SolutionRuleSetsInformationProviderTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private const string SolutionRoot = @"c:\solution";

        public SolutionRuleSetsInformationProviderTests()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
        }

        #region Tests
        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new SolutionRuleSetsInformationProvider(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void GetProjectRuleSetsDeclarations_WithNullProject_ThrowsArgumentNullException()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act
            Action act = () => testSubject.GetProjectRuleSetsDeclarations(null).ToArray();

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithDefaultValue()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(1, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            this.outputPane.AssertOutputStrings(0);
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithEmptyRuleSets()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", null);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", string.Empty);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(2, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            VerifyRuleSetInformation(info[1], prop2);
            this.outputPane.AssertOutputStrings(0);
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithSameNonDefaultValues()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", @"x:\Folder\Custom2.ruleset");
            PropertyMock prop3 = CreateProperty(this.projectMock, "config3", @"..\Custom3.ruleset");

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(3, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            VerifyRuleSetInformation(info[1], prop2);
            VerifyRuleSetInformation(info[2], prop3);
            this.outputPane.AssertOutputStrings(0);
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationWithNoRuleSetProperty()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            CreateProperty(this.projectMock, "config1", "Custom1.ruleset", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(0, "Unexpected number of results");
            this.outputPane.AssertOutputStrings(1);
        }


        [Fact]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_RuleSetsWithDirectories()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock ruleSet1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            CreateProperty(this.projectMock, "config1", @"x:\YYY\zzz", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
            PropertyMock ruleSet2 = CreateProperty(this.projectMock, "config2", "Custom1.ruleset");
            CreateProperty(this.projectMock, "config2", @"x:\YYY\zzz;q:\;", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(2, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], ruleSet1);
            VerifyRuleSetInformation(info[1], ruleSet2);
            this.outputPane.AssertOutputStrings(0);
        }

        [Fact]
        public void CalculateSolutionSonarQubeRuleSetFilePath_WithNullSonarQubeProjectKey_ThrowsArgumentNullException()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act
            Action act = () => testSubject.CalculateSolutionSonarQubeRuleSetFilePath(null, Language.CSharp);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnOpenSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            // Case 1: VB + invalid path characters
            // Act
            string ruleSetPath = testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey" + Path.GetInvalidPathChars().First(), Language.VBNET);

            // Assert
            ruleSetPath.Should().Be(@"z:\folder\solution\SonarQube\MyKey_VB.ruleset");

            // Case 2: C# + valid path characters
            // Act
            ruleSetPath = testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey", Language.CSharp);

            // Assert
            ruleSetPath.Should().Be(@"z:\folder\solution\SonarQube\MyKeyCSharp.ruleset");
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnClosedSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            // Act
            Action act = () => testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey", Language.CSharp);

            // Assert
            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnOpenSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Assert
            path.Should().Be(@"z:\folder\solution\SonarQube");
        }

        [Fact]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnClosedSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Assert
            path.Should().BeNull();
        }


        [Fact]
        public void SolutionRuleSetsInformationProvider_TryGetProjectRuleSetFilePath()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var fileSystem = new ConfigurableFileSystem();
            this.serviceProvider.RegisterService(typeof(IFileSystem), fileSystem);
            ProjectMock project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");
            RuleSetDeclaration declaration;
            string ruleSetPath;

            // Case 1: Declaration has an full path which exists on disk
            declaration = CreateDeclaration(project, @"c:\RuleSet.ruleset");
            fileSystem.RegisterFile(declaration.RuleSetPath);

            // Act
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath)
                .Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\RuleSet.ruleset");

            // Case 2: Declaration is relative to project and on disk
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, @"..\RuleSet.ruleset");
            fileSystem.RegisterFile(@"c:\Solution\RuleSet.ruleset");

            // Act
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath)
                .Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\Solution\RuleSet.ruleset");

            // Case 3: File doesn't exist
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, "MyFile.ruleset");

            // Act
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath)
                .Should().BeFalse();

            // Assert
            ruleSetPath.Should().BeNull();
        }
        #endregion

        #region Helpers
        private static RuleSetDeclaration CreateDeclaration(ProjectMock project, string ruleSetValue)
        {
            return new RuleSetDeclaration(project, new PropertyMock("never mind", null), ruleSetValue, "Configuration");
        }

        private static PropertyMock CreateProperty(ProjectMock project, string configurationName, object propertyValue, string propertyName = Constants.CodeAnalysisRuleSetPropertyKey)
        {
            ConfigurationMock config = GetOrCreateConfiguration(project, configurationName);

            var prop = config.Properties.RegisterKnownProperty(propertyName);
            prop.Value = propertyValue;
            return prop;
        }

        private static ConfigurationMock GetOrCreateConfiguration(ProjectMock project, string configurationName)
        {
            ConfigurationMock config = project.ConfigurationManager.Configurations.SingleOrDefault(c => c.ConfigurationName == configurationName);
            if (config == null)
            {
                config = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(config);
            }

            return config;
        }

        private void VerifyRuleSetInformation(RuleSetDeclaration info, PropertyMock property)
        {
            info.DeclaringProperty.Should().Be(property);
            info.RuleSetPath.Should().Be((string)property.Value);

            Configuration configuration = (Configuration)property.Collection.Parent;
            configuration.Should().NotBeNull("Test setup error, expected to have configuration as parent");

            info.ConfigurationContext.Should().Be(configuration.ConfigurationName);

            Property ruleSetDirectory = configuration.Properties.OfType<Property>().SingleOrDefault(p => p.Name == Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
            string ruleSetDirectoryValue = ruleSetDirectory?.Value as string;
            if (string.IsNullOrWhiteSpace(ruleSetDirectoryValue))
            {
                info.RuleSetDirectories.Should().HaveCount(0);
            }
            else
            {
                string[] expected = ruleSetDirectoryValue.Split(new[] { SolutionRuleSetsInformationProvider.RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries);
                info.RuleSetDirectories.Should().Equal(expected, "Actual: {0}", string.Join(", ", info.RuleSetDirectories));
            }
        }
        #endregion
    }
}
