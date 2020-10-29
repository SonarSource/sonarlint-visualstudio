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

using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionRuleSetsInformationProviderTests
    {
        private const string SolutionRoot = @"c:\solution";
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private SolutionRuleSetsInformationProvider testSubject;
        private MockFileSystem fileSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystem = new MockFileSystem();
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystemHelper);

            this.testSubject = new SolutionRuleSetsInformationProvider(serviceProvider, new SonarLintOutputLogger(serviceProvider), fileSystem);
        }

        #region Tests

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetsInformationProvider(null, new Mock<ILogger>().Object, new MockFileSystem()));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetsInformationProvider(Mock.Of<IServiceProvider>(), null, new MockFileSystem()));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetsInformationProvider(Mock.Of<IServiceProvider>(), new Mock<ILogger>().Object, null));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ArgChecks()
        {
            // Act + Assert
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectRuleSetsDeclarations(null).ToArray());
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithDefaultValue()
        {
            // Arrange
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);

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
        public void TryGetProjectRuleSetFilePath_DeclarationReferencesAbsolutePath_SameFilePathReturned()
        {
            // Arrange
            var project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");

            var declaration = CreateDeclaration(project, @"c:\RuleSet.ruleset");
            fileSystem.AddFile(declaration.RuleSetPath, new MockFileData(""));

            // Act
            testSubject.TryGetProjectRuleSetFilePath(declaration, out var ruleSetPath).Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\RuleSet.ruleset");
        }

        [TestMethod]
        public void TryGetProjectRuleSetFilePath_DeclarationReferencesRelativePath_FullPathReturned()
        {
            // Arrange
            var project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");

            var declaration = CreateDeclaration(project, @"..\RuleSet.ruleset");
            fileSystem.AddFile(@"c:\Solution\RuleSet.ruleset", new MockFileData(""));

            // Act
            testSubject.TryGetProjectRuleSetFilePath(declaration, out var ruleSetPath).Should().BeTrue();

            // Assert
            ruleSetPath.Should().Be(@"c:\Solution\RuleSet.ruleset");
        }

        [TestMethod]
        public void TryGetProjectRuleSetFilePath_DeclarationReferencesNonExistingFile_NullReturned()
        {
            // Arrange
            var project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");

            var declaration = CreateDeclaration(project, "MyFile.ruleset");

            // Act
            testSubject.TryGetProjectRuleSetFilePath(declaration, out var ruleSetPath).Should().BeFalse();

            // Assert
            ruleSetPath.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\n\r")]
        public void TryGetProjectRuleSetFilePath_DeclarationReferencesEmptyPath_NullReturned(string filePath)
        {
            // Arrange
            var project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");

            var declaration = CreateDeclaration(project, filePath);

            // Act
            testSubject.TryGetProjectRuleSetFilePath(declaration, out var ruleSetPath).Should().BeFalse();

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

            ((IVsBuildPropertyStorage)projectMock).GetPropertyValue(Constants.CodeAnalysisRuleSetDirectoriesPropertyKey, configuration.ConfigurationName, 0, out string ruleSetDirectoryValue);
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
