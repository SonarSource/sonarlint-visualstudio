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
using System.IO.Abstractions;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class CFamilyProjectBinderTests
    {
        private Mock<IServiceProvider> serviceProvider;
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IFileSystem> fileSystemMock;
        private TestLogger logger;
        private CFamilyProjectBinder testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();

            serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            fileSystemMock = new Mock<IFileSystem>();
            logger = new TestLogger();
            testSubject = new CFamilyProjectBinder(serviceProvider.Object, logger, fileSystemMock.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new CFamilyProjectBinder(null, Mock.Of<ILogger>(), Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullLogger_ArgumentNullException()
        {
            Action act = () => new CFamilyProjectBinder(serviceProvider.Object, null, Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new CFamilyProjectBinder(serviceProvider.Object, Mock.Of<ILogger>(), null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void IsBindingRequired_ProjectLanguageIsNotSupported_False()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetCSProjectKind();

            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://test.com"), "key", "name"),
                SonarLintMode.Connected, "c:\\");

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().Be(false);

            solutionRuleSetsInformationProviderMock.VerifyNoOtherCalls();
            fileSystemMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true, false, true)]
        [DataRow(false, true, true)]
        [DataRow(false, false, true)]
        [DataRow(true, true, false)]
        public void IsBindingRequired_ProjectHasTwoLanguages_ReturnsIfAllLanguagesHaveConfigFiles(bool isFirstLanguageBound, bool isSecondLanguageBound, bool expectedResult)
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            projectMock.SetProjectKind(new Guid(ProjectSystemHelper.CppProjectKind));

            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(new Uri("http://test.com"), "key", "name"),
                SonarLintMode.Connected, "c:\\");

            var cppFilePath = bindingConfiguration.BuildPathUnderConfigDirectory(Language.Cpp.FileSuffixAndExtension);
            var cFilePath = bindingConfiguration.BuildPathUnderConfigDirectory(Language.C.FileSuffixAndExtension);

            fileSystemMock
                .Setup(x => x.File.Exists(cppFilePath))
                .Returns(isFirstLanguageBound);

            if (isFirstLanguageBound)
            {
                fileSystemMock
                    .Setup(x => x.File.Exists(cFilePath))
                    .Returns(isSecondLanguageBound);
            }

            var result = testSubject.IsBindingRequired(bindingConfiguration, projectMock);
            result.Should().Be(expectedResult);

            fileSystemMock.VerifyAll();
        }

        [TestMethod]
        public void GetBindAction_LoggerWritten()
        {
            var bindAction = testSubject.GetBindAction(null, new ProjectMock("c:\\test.csproj"), CancellationToken.None);
            
            bindAction.Should().NotBeNull();
            logger.AssertPartialOutputStringExists("test.csproj");
        }
    }
}
