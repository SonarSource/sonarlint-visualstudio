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
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IRuleSetSerializer> ruleSetSerializerMock;
        private Mock<IProjectSystemHelper> projectSystemHelperMock;
        private Mock<IRuleSetReferenceChecker> rulesetReferenceCheckerMock;
        private Mock<IAdditionalFileReferenceChecker> additionalFileReferenceCheckerMock;

        private CSharpVBProjectBinder testSubject;
        private Mock<CSharpVBProjectBinder.CreateBindingOperationFunc> createBindingOperationFuncMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            ruleSetSerializerMock = new Mock<IRuleSetSerializer>();
            projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            createBindingOperationFuncMock = new Mock<CSharpVBProjectBinder.CreateBindingOperationFunc>();

            serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(ruleSetSerializerMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            rulesetReferenceCheckerMock = new Mock<IRuleSetReferenceChecker>();
            additionalFileReferenceCheckerMock = new Mock<IAdditionalFileReferenceChecker>();

            testSubject = new CSharpVBProjectBinder(serviceProviderMock.Object, fileSystemMock.Object, rulesetReferenceCheckerMock.Object, additionalFileReferenceCheckerMock.Object, createBindingOperationFuncMock.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            using (new AssertIgnoreScope())
            {
                Action act = () => new CSharpVBProjectBinder(null, Mock.Of<IFileSystem>());

                act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
            }
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            using (new AssertIgnoreScope())
            {
                Action act = () => new CSharpVBProjectBinder(serviceProviderMock.Object, null);

                act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
            }
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

            bindingOperationMock.Verify(x => x.Initialize(), Times.Once);
            bindingOperationMock.Verify(x => x.Prepare(CancellationToken.None), Times.Once);
            bindingOperationMock.Verify(x => x.Commit(), Times.Never);

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

            VerifySolutionAdditionalFileNotChecked(bindingConfiguration);
            VerifySolutionRulesetNotChecked(bindingConfiguration);
            VerifyProjectAdditionalFileNotChecked();
            VerifyProjectRulesetsNotChecked();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionHasNoAdditionalFile_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            var mockAdditionalFilePath = GetSolutionAdditionalFilePath(bindingConfiguration);
            fileSystemMock.Setup(x => x.File.Exists(mockAdditionalFilePath)).Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            fileSystemMock.VerifyAll();

            VerifySolutionRulesetNotChecked(bindingConfiguration);
            VerifyProjectAdditionalFileNotChecked();
            VerifyProjectRulesetsNotChecked();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionHasNoRuleset_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            SetupSolutionAdditionalFileExists(bindingConfiguration);

            var mockRulesetPath = GetSolutionRulesetPath(bindingConfiguration);
            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            fileSystemMock.VerifyAll();

            VerifyProjectAdditionalFileNotChecked();
            VerifyProjectRulesetsNotChecked();
        }

        [TestMethod]
        public void IsBindingRequired_SolutionHasRulesetButFileFailsToLoad_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            SetupSolutionAdditionalFileExists(bindingConfiguration);
            SetupSolutionRulesetExists(bindingConfiguration, null);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            ruleSetSerializerMock.VerifyAll();

            VerifyProjectAdditionalFileNotChecked();
            VerifyProjectRulesetsNotChecked();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectDoesNotReferenceAdditionalFile_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            SetupSolutionRulesetExists(bindingConfiguration, new RuleSet("test"));
            var solutionAdditionalFilePath = SetupSolutionAdditionalFileExists(bindingConfiguration);

            additionalFileReferenceCheckerMock
                .Setup(x => x.IsReferenced(projectMock, solutionAdditionalFilePath))
                .Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            additionalFileReferenceCheckerMock.VerifyAll();
            VerifyProjectRulesetsNotChecked();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectDoesNotReferenceSolutionRuleset_True()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            var solutionAdditionalFilePath = GetSolutionAdditionalFilePath(bindingConfiguration);
            fileSystemMock.Setup(x => x.File.Exists(solutionAdditionalFilePath)).Returns(true);

            additionalFileReferenceCheckerMock
                .Setup(x => x.IsReferenced(projectMock, solutionAdditionalFilePath))
                .Returns(true);

            var solutionRuleSet = new RuleSet("test");
            SetupSolutionRulesetExists(bindingConfiguration, solutionRuleSet);

            rulesetReferenceCheckerMock
                .Setup(x => x.IsReferenced(projectMock, solutionRuleSet))
                .Returns(false);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeTrue();

            additionalFileReferenceCheckerMock.VerifyAll();
            rulesetReferenceCheckerMock.VerifyAll();
        }

        [TestMethod]
        public void IsBindingRequired_ProjectReferencesSolutionRulesetAndAdditionalFile_False()
        {
            var bindingConfiguration = GetBindingConfiguration();
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            var solutionRuleSet = new RuleSet("test");
            SetupSolutionRulesetExists(bindingConfiguration, solutionRuleSet);

            rulesetReferenceCheckerMock
                .Setup(x => x.IsReferenced(projectMock, solutionRuleSet))
                .Returns(true);

            var solutionAdditionalFilePath = SetupSolutionAdditionalFileExists(bindingConfiguration);

            additionalFileReferenceCheckerMock
                .Setup(x => x.IsReferenced(projectMock, solutionAdditionalFilePath))
                .Returns(true);

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().BeFalse();
        }

        private string GetSolutionRulesetPath(BindingConfiguration bindingConfiguration)
        {
            return bindingConfiguration.BuildPathUnderConfigDirectory(Language.CSharp.FileSuffixAndExtension);
        }

        private string GetSolutionAdditionalFilePath(BindingConfiguration bindingConfiguration)
        {
            var directory = bindingConfiguration.BuildPathUnderConfigDirectory();
            
            return Path.Combine(directory, Language.CSharp.Id, "SonarLint.xml");
        }

        private static BindingConfiguration GetBindingConfiguration()
        {
            var bindingConfiguration =
                new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://a.com"), "key", "name"),
                    SonarLintMode.Connected, "dummy directory");

            return bindingConfiguration;
        }

        private void SetupSolutionRulesetExists(BindingConfiguration bindingConfiguration, RuleSet ruleSet)
        {
            var mockRulesetPath = GetSolutionRulesetPath(bindingConfiguration);
            fileSystemMock.Setup(x => x.File.Exists(mockRulesetPath)).Returns(true);

            ruleSetSerializerMock.Setup(x => x.LoadRuleSet(mockRulesetPath)).Returns(ruleSet);
        }

        private string SetupSolutionAdditionalFileExists(BindingConfiguration bindingConfiguration)
        {
            var solutionAdditionalFilePath = GetSolutionAdditionalFilePath(bindingConfiguration);
            fileSystemMock.Setup(x => x.File.Exists(solutionAdditionalFilePath)).Returns(true);

            return solutionAdditionalFilePath;
        }

        private void VerifySolutionAdditionalFileNotChecked(BindingConfiguration bindingConfiguration)
        {
            var additionalFilePath = GetSolutionAdditionalFilePath(bindingConfiguration);
            fileSystemMock.Verify(x => x.File.Exists(additionalFilePath), Times.Never);
        }

        private void VerifySolutionRulesetNotChecked(BindingConfiguration bindingConfiguration)
        {
            var rulesetFilePath = GetSolutionRulesetPath(bindingConfiguration);
            fileSystemMock.Verify(x=> x.File.Exists(rulesetFilePath), Times.Never);

            VerifySolutionRulesetNotLoaded();
        }

        private void VerifySolutionRulesetNotLoaded()
        {
            ruleSetSerializerMock.VerifyNoOtherCalls();
        }

        private void VerifyProjectRulesetsNotChecked()
        {
            rulesetReferenceCheckerMock.VerifyNoOtherCalls();
        }

        private void VerifyProjectAdditionalFileNotChecked()
        {
            additionalFileReferenceCheckerMock.VerifyNoOtherCalls();
        }
    }
}
