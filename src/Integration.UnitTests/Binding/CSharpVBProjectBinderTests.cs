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
using EnvDTE;
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
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IRuleSetSerializer> ruleSetSerializerMock;

        private CSharpVBProjectBinder testSubject;
        private Mock<CSharpVBProjectBinder.CreateBindingOperationFunc> createBindingOperationFuncMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();
            ruleSetSerializerMock = new Mock<IRuleSetSerializer>();
            createBindingOperationFuncMock = new Mock<CSharpVBProjectBinder.CreateBindingOperationFunc>();

            serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(ruleSetSerializerMock.Object);

            testSubject = new CSharpVBProjectBinder(serviceProviderMock.Object, fileSystemMock.Object, createBindingOperationFuncMock.Object);
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
        public void GetBindAction_CallsInitializeAndPrepare_ReturnsCommitAction()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            var bindingConfig = new CSharpVBBindingConfig(new RuleSet("test"), "c:\\test.ruleset");
            
            var bindingOperationMock = new Mock<ICSharpVBBindingOperation>();
            
            createBindingOperationFuncMock
                .Setup(x => x(projectMock, bindingConfig))
                .Returns(bindingOperationMock.Object);

            var bindAction = testSubject.GetBindAction(bindingConfig, projectMock, CancellationToken.None);

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
        public void IsBindingRequired_SolutionRulesetFileDoesNotExist_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifyProjectRulesetsNotFetched();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionRulesetFileExistsButFailsToLoad_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);
            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(null as RuleSet);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            VerifyProjectRulesetsNotFetched();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectHasNoRulesets_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);
            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(new RuleSet("test"));

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
            var projectMock = GetCSharpProject();
            var mockRulesetPath = SetupRulesetPath(bindingConfiguration);

            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);

            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(new RuleSet("test"));

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

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.CalculateSolutionSonarQubeRuleSetFilePath(bindingConfiguration.Project.ProjectKey, Language.CSharp, bindingConfiguration.Mode))
                .Returns(mockRulesetPath);

            return mockRulesetPath;
        }

        private static BindingConfiguration GetBindingConfiguration()
        {
            var bindingConfiguration =
                new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://a.com"), "key", "name"),
                    SonarLintMode.Connected, "dummy directory");

            return bindingConfiguration;
        }

        private static ProjectMock GetCSharpProject()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();
            return projectMock;
        }

        private void VerifyProjectRulesetsNotFetched()
        {
            solutionRuleSetsInformationProviderMock
                .Verify(x => x.GetProjectRuleSetsDeclarations(It.IsAny<Project>()),
                    Times.Never());
        }
    }
}
