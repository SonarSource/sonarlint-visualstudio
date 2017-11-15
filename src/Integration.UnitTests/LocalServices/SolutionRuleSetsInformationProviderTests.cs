/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionRuleSetsInformationProviderTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private const string SolutionRoot = @"c:\solution";

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystemHelper);
        }

        #region Tests

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetsInformationProvider(null));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ArgChecks()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectRuleSetsDeclarations(null).ToArray());
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationWithNoRuleSetProperty()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            CreateProperty(this.projectMock, "config1", "Custom1.ruleset", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().BeEmpty("Unexpected number of results");
            this.outputPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_RuleSetsWithDirectories()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock ruleSet1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            SetBuildProperty(this.projectSystemHelper, this.projectMock, Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, @"x:\YYY\zzz", "config1");

            PropertyMock ruleSet2 = CreateProperty(this.projectMock, "config2", "Custom1.ruleset");
            SetBuildProperty(this.projectSystemHelper, this.projectMock, Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, @"x:\YYY\zzz;q:\;", "config2");

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Assert
            info.Should().HaveCount(2, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], ruleSet1);
            VerifyRuleSetInformation(info[1], ruleSet2);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_ArgChecks()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath(null, Language.CSharp));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath(null, Language.VBNET));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnOpenSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            this.projectSystemHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

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

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnClosedSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            this.projectSystemHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);

            // Act + Assert
            Exceptions.Expect<InvalidOperationException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey", Language.CSharp));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnOpenSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            this.projectSystemHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Assert
            path.Should().Be(@"z:\folder\solution\SonarQube");
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnClosedSolution()
        {
            // Arrange
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            this.projectSystemHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Assert
            path.Should().BeNull();
        }

        [TestMethod]
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
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath).Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\RuleSet.ruleset");

            // Case 2: Declaration is relative to project and on disk
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, @"..\RuleSet.ruleset");
            fileSystem.RegisterFile(@"c:\Solution\RuleSet.ruleset");

            // Act
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath).Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\Solution\RuleSet.ruleset");

            // Case 3: File doesn't exist
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, "MyFile.ruleset");

            // Act
            testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath).Should().BeFalse();

            // Assert
            ruleSetPath.Should().BeNull();
        }

        #endregion Tests

        #region Helpers

        private static RuleSetDeclaration CreateDeclaration(ProjectMock project, string ruleSetValue)
        {
            return new RuleSetDeclaration(project, new PropertyMock("never mind", null), ruleSetValue, "Configuration");
        }

        private static void SetBuildProperty(ConfigurableVsProjectSystemHelper projectSystemHelper, ProjectMock project, string propertyName, string propertyValue, string configurationName)
        {
            projectSystemHelper.SetProjectProperty(project, propertyName, propertyValue, configurationName);
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
            if (configuration == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Test setup error, expected to have configuration as parent");
            }

            info.ConfigurationContext.Should().Be(configuration.ConfigurationName);

            string ruleSetDirectoryValue = null;
            ((IVsBuildPropertyStorage)projectMock).GetPropertyValue(Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, configuration.ConfigurationName, 0, out ruleSetDirectoryValue);
            if (string.IsNullOrWhiteSpace(ruleSetDirectoryValue))
            {
                info.RuleSetDirectories.Should().BeEmpty();
            }
            else
            {
                string[] expected = ruleSetDirectoryValue.Split(new[] { SolutionRuleSetsInformationProvider.RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries);
                CollectionAssert.AreEquivalent(expected, info.RuleSetDirectories.ToArray(), "Actual: {0}", string.Join(", ", info.RuleSetDirectories));
            }
        }

        #endregion Helpers
    }
}