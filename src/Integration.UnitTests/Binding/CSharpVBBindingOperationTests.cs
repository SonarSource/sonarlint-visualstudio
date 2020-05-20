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
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.Binding;
using Language = SonarLint.VisualStudio.Core.Language;
using RuleSet = Microsoft.VisualStudio.CodeAnalysis.RuleSets.RuleSet;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public partial class CSharpVBBindingOperationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private const string SolutionRoot = @"c:\solution";
        private CSharpVBBindingConfig cSharpVBBindingConfig;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private ConfigurableRuleSetSerializer ruleSetFS;
        private MockFileSystem fileSystem;
        private Mock<IAdditionalFileConflictChecker> additionalFileConflictChecker;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystem = new MockFileSystem();

            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));
            this.outputPane = new ConfigurableVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem(fileSystem);
            this.ruleSetFS = new ConfigurableRuleSetSerializer(fileSystem);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleSetFS);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider),
                new SolutionRuleSetsInformationProvider(this.serviceProvider, new Mock<ILogger>().Object,  new MockFileSystem()));
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);

            var ruleset = new FilePathAndContent<RuleSet>(@"c:\Solution\sln.ruleset", new RuleSet("SonarQube"));
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>(@"c:\Solution\additionalFile.txt", new SonarLintConfiguration());
            cSharpVBBindingConfig = new CSharpVBBindingConfig(ruleset, additionalFile);

            additionalFileConflictChecker = new Mock<IAdditionalFileConflictChecker>();
        }

        #region Tests

        [TestMethod]
        public void ProjectBindingOperation_ArgChecks()
        {
            var logger = new TestLogger();
            Exceptions.Expect<ArgumentNullException>(() => new CSharpVBBindingOperation(null, this.projectMock, this.cSharpVBBindingConfig));
            Exceptions.Expect<ArgumentNullException>(() => new CSharpVBBindingOperation(this.serviceProvider, null, this.cSharpVBBindingConfig));
            Exceptions.Expect<ArgumentNullException>(() => new CSharpVBBindingOperation(this.serviceProvider, this.projectMock, null));

            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            testSubject.Should().NotBeNull("Suppress warning that not used");
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithDefaultValues()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", CSharpVBBindingOperation.DefaultProjectRuleSet);

            // Act
            testSubject.Initialize();

            // Assert
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath.Should().Be(CSharpVBBindingOperation.DefaultProjectRuleSet);
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithEmptyRuleSets()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", null);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", string.Empty);

            // Act
            testSubject.Initialize();

            // Assert
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                string.IsNullOrEmpty(testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath).Should().BeTrue();
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithSameNonDefaultValues()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", "Custom1.ruleset");

            // Act
            testSubject.Initialize();

            // Assert
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath.Should().Be("Custom1.ruleset");
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertiesWithVariousValues()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", "NonDefualtRuleSet.ruleset");

            // Act
            testSubject.Initialize();

            // Assert
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.CSharp);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            testSubject.PropertyInformationMap[prop1].CurrentRuleSetFilePath.Should().Be(CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.PropertyInformationMap[prop1].TargetRuleSetFileName.Should().Be("project", "Default ruleset - expected project based name to be generated");
            testSubject.PropertyInformationMap[prop2].CurrentRuleSetFilePath.Should().Be("NonDefualtRuleSet.ruleset");
            testSubject.PropertyInformationMap[prop2].TargetRuleSetFileName.Should().Be("project.config2", "Non default ruleset - expected configuration based rule set name to be generated");
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_VariousRuleSetsInProjects()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock customRuleSetProperty1 = CreateProperty(this.projectMock, "config1", "Custom.ruleset");
            PropertyMock customRuleSetProperty2 = CreateProperty(this.projectMock, "config2", "Custom.ruleset");
            PropertyMock defaultRuleSetProperty1 = CreateProperty(this.projectMock, "config3", CSharpVBBindingOperation.DefaultProjectRuleSet);
            PropertyMock defaultRuleSetProperty2 = CreateProperty(this.projectMock, "config4", CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().Be(null);
            testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected all the properties with default ruleset to have the same new ruleset");
            testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected all the properties with default ruleset to have the same new ruleset");

            string expectedRuleSetForConfig1 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config1.ruleset");
            testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetForConfig1, "Expected different rule set path for properties with custom rulesets");
            fileSystem.GetFile(expectedRuleSetForConfig1).Should().Be(null);

            string expectedRuleSetForConfig2 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config2.ruleset");
            testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetForConfig2, "Expected different rule set path for properties with custom rulesets");
            fileSystem.GetFile(expectedRuleSetForConfig2).Should().Be(null);

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert that written
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().NotBe(null);
            fileSystem.GetFile(expectedRuleSetForConfig1).Should().NotBe(null);
            fileSystem.GetFile(expectedRuleSetForConfig2).Should().NotBe(null);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_SameNonDefaultRuleSetsInProject()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock customRuleSetProperty1 = CreateProperty(this.projectMock, "config1", "Custom.ruleset");
            PropertyMock customRuleSetProperty2 = CreateProperty(this.projectMock, "config2", "Custom.ruleset");
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().Be(null);
            testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");
            testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert that written
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().NotBe(null);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_SameDefaultRuleSetsInProject()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock defaultRuleSetProperty1 = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            PropertyMock defaultRuleSetProperty2 = CreateProperty(this.projectMock, "config2", CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);


            // Assert
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().Be(null);
            testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");
            testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert that written
            fileSystem.GetFile(expectedRuleSetFileForPropertiesWithDefaultRulSets).Should().NotBe(null);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_Cancellation()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                CancellationToken token = src.Token;
                src.Cancel();

                // Act
                testSubject.Prepare(token);
            }

            // Assert
            string expectedFile = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            testSubject.PropertyInformationMap[prop].NewRuleSetFilePath.Should().BeNull("Not expecting the new rule set path to be set when canceled");
            prop.Value.ToString().Should().Be(CSharpVBBindingOperation.DefaultProjectRuleSet, "Should not update the property value");
            this.projectMock.Files.ContainsKey(expectedFile).Should().BeFalse("Should not be added to the project");
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_AdditionalFileAlreadyReferenced_AdditionalFileConflictNotChecked()
        {
            var testSubject = CreateTestSubject();
            projectMock.SetCSProjectKind();
            testSubject.Initialize();

            projectSystemHelper.IsFileInProjectAction = (project, s) => project == projectMock && s == cSharpVBBindingConfig.AdditionalFile.Path;

            testSubject.Prepare(CancellationToken.None);

            additionalFileConflictChecker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_AdditionalFileConflict_Exception()
        {
            var testSubject = CreateTestSubject();
            projectMock.SetCSProjectKind();
            testSubject.Initialize();

            var additionalFileName = Path.GetFileName(cSharpVBBindingConfig.AdditionalFile.Path);
            var conflictingPath = "this is the conflicting file path";
            additionalFileConflictChecker
                .Setup(x => x.HasConflictingAdditionalFile(projectMock, additionalFileName, out conflictingPath))
                .Returns(true);

            Action act = () => testSubject.Prepare(CancellationToken.None);

            act.Should().ThrowExactly<SonarLintException>().And.Message.Should().Contain(((Project)projectMock).Name);
            act.Should().ThrowExactly<SonarLintException>().And.Message.Should().Contain(additionalFileName);
            act.Should().ThrowExactly<SonarLintException>().And.Message.Should().Contain(conflictingPath);

            additionalFileConflictChecker.VerifyAll();
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_NoAdditionalFileConflict_NoException()
        {
            var testSubject = CreateTestSubject();
            projectMock.SetCSProjectKind();
            testSubject.Initialize();

            var additionalFileName = Path.GetFileName(cSharpVBBindingConfig.AdditionalFile.Path);
            var conflictingPath = "this is the conflicting file path";
            additionalFileConflictChecker
                .Setup(x => x.HasConflictingAdditionalFile(projectMock, additionalFileName, out conflictingPath))
                .Returns(false);

            Action act = () => testSubject.Prepare(CancellationToken.None);

            act.Should().NotThrow();

            additionalFileConflictChecker.VerifyAll();
        }

        [TestMethod]
        public void ProjectBindingOperation_Commit_NewProjectSystem_DoesNotAddFile()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            this.projectSystemHelper.SetIsLegacyProjectSystem(false);

            // Act
            using (new AssertIgnoreScope()) // Ignore that the file is not on disk
            {
                testSubject.Commit();
            }

            // Assert
            string expectedFile = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            prop.Value.ToString().Should().Be(Path.GetFileName(expectedFile), "Should update the property value");
            this.projectMock.Files.ContainsKey(expectedFile).Should().BeFalse("Should not add the file to the project for the new project system");
        }

        [TestMethod]
        public void ProjectBindingOperation_Commit_LegacyProjectSystem_DoesAddFile()
        {
            // Arrange
            CSharpVBBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop = CreateProperty(this.projectMock, "config1", CSharpVBBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            this.projectSystemHelper.SetIsLegacyProjectSystem(true);

            // Act
            using (new AssertIgnoreScope()) // Ignore that the file is not on disk
            {
                testSubject.Commit();
            }

            // Assert
            string projectFile = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            prop.Value.ToString().Should().Be(Path.GetFileName(projectFile), "Should update the property value");
            this.projectMock.Files.ContainsKey(projectFile).Should().BeTrue("Should add the file to the project for the legacy project system");
        }

        [TestMethod]
        public void ProjectBindingOperation_Commit_AdditionalFileIsNotRefd_FileIsAdded()
        {
            // Arrange
            projectMock.SetCSProjectKind();
            projectMock.Files.Count().Should().Be(0); // sanity check - no files

            var testSubject = CreateTestSubject();
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            testSubject.Commit();

            // Assert
            CheckAdditionalFileIsReferenced(projectMock, cSharpVBBindingConfig.AdditionalFile.Path);
            projectMock.Files.Count().Should().Be(1); // file added
        }

        [TestMethod]
        [DataRow("WrongItemType")]
        [DataRow(Constants.AdditionalFilesItemTypeName)]
        public void ProjectBindingOperation_Commit_FileIsAlreadyReferenced(string existingFileItemType)
        {
            // Arrange
            var filePath = cSharpVBBindingConfig.AdditionalFile.Path;
            projectMock.SetCSProjectKind();
            projectMock.AddProjectItem(filePath, existingFileItemType);

            var testSubject = CreateTestSubject();
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            testSubject.Commit();

            // Assert
            CheckAdditionalFileIsReferenced(projectMock, filePath);
            projectMock.Files.Count().Should().Be(1); // existing file updated
        }

        [TestMethod]
        public void AddAdditional_NonCriticalException_IsCaughtAndWrapped()
        {
            var projectMock = new ProjectMock("any.proj");
            var innerException = new InvalidCastException("inner exception message");

            var projectSystemMock = new Mock<IProjectSystemHelper>();
            projectSystemMock.Setup(x => x.AddFileToProject(projectMock, It.IsAny<string>(), Constants.AdditionalFilesItemTypeName))
                .Throws(innerException);

            Action act = () => CSharpVBBindingOperation.AddAdditionalFileToProject(projectSystemMock.Object, projectMock, "anyFile.txt");
            act.Should().ThrowExactly<SonarLintException>()
                .Where(x => x.Message.Contains(projectMock.FilePath)
                            && x.Message.Contains(innerException.Message))
                .And.InnerException.Should().BeSameAs(innerException);
        }

        [TestMethod]
        public void AddAdditional_CriticalException_IsNotCaught()
        {
            var projectMock = Mock.Of<EnvDTE.Project>();

            var projectSystemMock = new Mock<IProjectSystemHelper>();
            projectSystemMock.Setup(x => x.AddFileToProject(projectMock, It.IsAny<string>(), It.IsAny<string>()))
                .Throws<StackOverflowException>();

            Action act = () => CSharpVBBindingOperation.AddAdditionalFileToProject(projectSystemMock.Object, projectMock, "any file");
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void EnsureItemTypeIsCorrect_NonCriticalException_IsCaughtAndWrapped()
        {
            var projectMock = new ProjectMock("myproject.vbproj");
            var innerException = new ArgumentException("aaa bbb");

            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(x => x.ContainingProject).Returns(projectMock);
            projectItemMock.Setup(x => x.Properties).Throws(innerException);

            Action act = () => CSharpVBBindingOperation.EnsureItemTypeIsCorrect(projectItemMock.Object);

            act.Should().ThrowExactly<SonarLintException>()
                .Where(x => x.Message.Contains(projectMock.FilePath)
                            && x.Message.Contains(innerException.Message))
                .And.InnerException.Should().BeSameAs(innerException);
        }

        [TestMethod]
        public void EnsureItemTypeIsCorrect_CriticalException_IsNotCaught()
        {
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(x => x.Properties).Throws<AccessViolationException>();

            Action act = () => CSharpVBBindingOperation.EnsureItemTypeIsCorrect(projectItemMock.Object);
            act.Should().ThrowExactly<AccessViolationException>();
        }

        #endregion Tests

        #region Helpers

        private static PropertyMock CreateProperty(ProjectMock project, string configurationName, object propertyValue)
        {
            ConfigurationMock config = project.ConfigurationManager.Configurations.SingleOrDefault(c => c.ConfigurationName == configurationName);
            if (config == null)
            {
                config = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(config);
            }

            var prop = config.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            prop.Value = propertyValue;
            return prop;
        }

        private CSharpVBBindingOperation CreateTestSubject()
        {
            return new CSharpVBBindingOperation(serviceProvider, projectMock, cSharpVBBindingConfig, new MockFileSystem(), additionalFileConflictChecker.Object);
        }

        private static void CheckAdditionalFileIsReferenced(ProjectMock projectMock, string filePath)
        {
            projectMock.Files.ContainsKey(filePath).Should().BeTrue();
            projectMock.ProjectItemsMock[filePath].PropertiesMock[Constants.ItemTypePropertyKey].Value.Should().Be(Constants.AdditionalFilesItemTypeName);
        }

        #endregion Helpers
    }
}
