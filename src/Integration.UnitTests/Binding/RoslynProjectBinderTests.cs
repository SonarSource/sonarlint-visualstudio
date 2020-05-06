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
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class RoslynProjectBinderTests
    {
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IFileSystem> fileSystemMock;

        private RoslynProjectBinder testSubject;
        private Mock<RoslynProjectBinder.CreateBindingOperationFunc> createBindingOperationFuncMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();
            createBindingOperationFuncMock = new Mock<RoslynProjectBinder.CreateBindingOperationFunc>();

            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(Mock.Of<IRuleSetSerializer>());

            testSubject = new RoslynProjectBinder(serviceProviderMock.Object, fileSystemMock.Object, createBindingOperationFuncMock.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new RoslynProjectBinder(null, Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }


        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new RoslynProjectBinder(serviceProviderMock.Object, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void GetBindAction_CallsInitializeAndPrepare_ReturnsCommitAction()
        {
            var projectMock = new ProjectMock("c:\\test.csproj");
            var bindingConfig = new DotNetBindingConfigFile(new RuleSet("test"), "c:\\test.ruleset");
            
            var bindingOperationMock = new Mock<IBindingOperation>();
            
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
    }
}
