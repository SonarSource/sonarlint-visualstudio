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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class CSharpVBProjectBinderTests
    {
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<ISolutionBindingFilePathGenerator> solutionBindingFilePathGeneratorMock;
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IRuleSetSerializer> ruleSetSerializerMock;
        private Mock<IProjectSystemHelper> projectSystemHelperMock;

        private CSharpVBProjectBinder testSubject;
        private Mock<CSharpVBProjectBinder.CreateBindingOperationFunc> createBindingOperationFuncMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();
            ruleSetSerializerMock = new Mock<IRuleSetSerializer>();
            projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            createBindingOperationFuncMock = new Mock<CSharpVBProjectBinder.CreateBindingOperationFunc>();

            serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(ruleSetSerializerMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            solutionBindingFilePathGeneratorMock = new Mock<ISolutionBindingFilePathGenerator>();

            testSubject = new CSharpVBProjectBinder(serviceProviderMock.Object, fileSystemMock.Object, solutionBindingFilePathGeneratorMock.Object, createBindingOperationFuncMock.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new CSharpVBProjectBinder(null, Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new CSharpVBProjectBinder(serviceProviderMock.Object, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Ctor_NullSolutionBindingFilePathGenerator_ArgumentNullException()
        {
            Action act = () => new CSharpVBProjectBinder(serviceProviderMock.Object, fileSystemMock.Object, null, createBindingOperationFuncMock.Object);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingFilePathGenerator");
        }

        [TestMethod]
        public void GetBindAction_CallsInitializeAndPrepare_ReturnsCommitAction()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            var bindingConfig = new Mock<ICSharpVBBindingConfig>();
            var bindingOperationMock = new Mock<ICSharpVBBindingOperation>();
            
            createBindingOperationFuncMock
                .Setup(x => x(projectMock, bindingConfig.Object))
                .Returns(bindingOperationMock.Object);

            var bindAction = testSubject.GetBindAction(bindingConfig.Object, projectMock, CancellationToken.None);

            bindingOperationMock.Verify(x=> x.Initialize(), Times.Once);
            bindingOperationMock.Verify(x=> x.Prepare(CancellationToken.None), Times.Once);
            bindingOperationMock.Verify(x=> x.Commit(), Times.Never);

            bindAction();

            bindingOperationMock.Verify(x => x.Commit(), Times.Once);
        }

        [TestMethod]
        public void IsBindingRequired_ProjectLanguageIsNotSupported_False()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetProjectKind(new Guid(ProjectSystemHelper.CppProjectKind));

            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://test.com"), "key", "name"),
                SonarLintMode.Connected, "c:\\");

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().Be(false);

            solutionRuleSetsInformationProviderMock.VerifyNoOtherCalls();
            fileSystemMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionHasNoAdditionalFile_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockAdditionalFilePath = SetupAdditionalFilePath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockAdditionalFilePath)).Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifySolutionRulesetNotLoaded();
            VerifyProjectRulesetsNotLoaded();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionRulesetFileDoesNotExist_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);
            var mockAdditionalFilePath = SetupAdditionalFilePath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockAdditionalFilePath)).Returns(true);
            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifySolutionRulesetNotLoaded();
            VerifyProjectRulesetsNotLoaded();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionRulesetFileExistsButFailsToLoad_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);
            var mockAdditionalFilePath = SetupAdditionalFilePath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockAdditionalFilePath)).Returns(true);
            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);
            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(null as RuleSet);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifyProjectRulesetsNotLoaded();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectHasNoAdditionalFiles_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            SetupSolutionIsBoundCorrectly(bindingConfiguration);

            var projectMock = GetCSharpProject(hasAdditionalFile:false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifyProjectRulesetsNotLoaded();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectHasAdditionalFileWithWrongName_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            SetupSolutionIsBoundCorrectly(bindingConfiguration);

            var projectMock = GetCSharpProject(additionalFileName: "wrong additional file name");

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifyProjectRulesetsNotLoaded();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectHasNoRulesets_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            SetupSolutionIsBoundCorrectly(bindingConfiguration);

            var projectMock = GetCSharpProject();
         
            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(Array.Empty<RuleSetDeclaration>());

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectHasOneRuleset_CantLoadProjectRulesetFile_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            SetupSolutionIsBoundCorrectly(bindingConfiguration);

            var projectMock = GetCSharpProject();
            var ruleSetDeclaration = GetRuleSetDeclaration(projectMock);

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(new List<RuleSetDeclaration> {ruleSetDeclaration});

            var filePath = "";
            solutionRuleSetsInformationProviderMock
                .Setup(x => x.TryGetProjectRuleSetFilePath(projectMock, ruleSetDeclaration, out filePath))
                .Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsBindingRequired_ProjectHasOneRuleset_ReturnIfReferencesSolutionRuleset(bool referencesSolutionRuleset)
        {
            Assert.Inconclusive("TBD");
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        [DataRow(false, false, false)]
        public void IsBindingRequired_ProjectHasTwoRulesets_ReturnIfAllReferenceSolutionRuleset(bool firstReferencesSolutionRuleset, bool secondReferencesSolutionRuleset, bool expectedResult)
        {
            Assert.Inconclusive("TBD");
        }

        private RuleSetDeclaration GetRuleSetDeclaration(ProjectMock projectMock)
        {
            var mockDeclaration =
                new RuleSetDeclaration(projectMock, new PropertyMock("name", null), "test path", null);
            
            return mockDeclaration;
        }

        private string SetupRulesetPath(BindingConfiguration bindingConfiguration)
        {
            var mockRulesetPath = "c:\\test.ruleset";

            solutionBindingFilePathGeneratorMock
                .Setup(x => x.Generate(bindingConfiguration.BindingConfigDirectory, bindingConfiguration.Project.ProjectKey, Language.CSharp.FileSuffixAndExtension))
                .Returns(mockRulesetPath);

            return mockRulesetPath;
        }

        private string SetupAdditionalFilePath(BindingConfiguration bindingConfiguration)
        {
            var mockAdditionalFilePath = "c:\\test.xml";

            solutionBindingFilePathGeneratorMock
                .Setup(x => x.Generate(bindingConfiguration.BindingConfigDirectory, bindingConfiguration.Project.ProjectKey, "csharp\\SonarLint.xml"))
                .Returns(mockAdditionalFilePath);

            return mockAdditionalFilePath;
        }

        private static BindingConfiguration GetBindingConfiguration()
        {
            var bindingConfiguration =
                new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://a.com"), "key", "name"),
                    SonarLintMode.Connected, "dummy directory");

            return bindingConfiguration;
        }

        private ProjectMock GetCSharpProject(bool hasAdditionalFile = true, string additionalFileName = "csharp\\SonarLint.xml")
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            projectSystemHelperMock
                .Setup(x => x.DoesExistInItemGroup(projectMock, "AdditionalFiles", additionalFileName))
                .Returns(hasAdditionalFile);

            return projectMock;
        }

        private void SetupSolutionIsBoundCorrectly(BindingConfiguration bindingConfiguration)
        {
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);
            var mockAdditionalFilePath = SetupAdditionalFilePath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockAdditionalFilePath)).Returns(true);
            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);
            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(new RuleSet("test"));
        }

        private void VerifySolutionRulesetNotLoaded()
        {
            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        private void VerifyProjectRulesetsNotLoaded()
        {
            solutionRuleSetsInformationProviderMock.VerifyNoOtherCalls();
        }
    }
}
