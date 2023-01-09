/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class ProjectBinderFactoryTests
    {
        private Mock<IProjectToLanguageMapper> projectToLanguageMapper;
        private ProjectBinderFactory testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            projectToLanguageMapper = new Mock<IProjectToLanguageMapper>();

            var serviceProvider = new ConfigurableServiceProvider();
            var mefHost = ConfigurableComponentModel.CreateWithExports(MefTestHelpers.CreateExport<IProjectToLanguageMapper>(projectToLanguageMapper.Object));
            serviceProvider.RegisterService(typeof(SComponentModel), mefHost);
            serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), Mock.Of<ISolutionRuleSetsInformationProvider>());
            serviceProvider.RegisterService(typeof(IRuleSetSerializer), Mock.Of<IRuleSetSerializer>());
            serviceProvider.RegisterService(typeof(IProjectSystemHelper), Mock.Of<IProjectSystemHelper>());

            testSubject = new ProjectBinderFactory(serviceProvider, Mock.Of<ILogger>(), Mock.Of<IFileSystem>());
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new ProjectBinderFactory(null, Mock.Of<ILogger>(), Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullLogger_ArgumentNullException()
        {
            Action act = () => new ProjectBinderFactory(Mock.Of<IServiceProvider>(), null, Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new ProjectBinderFactory(Mock.Of<IServiceProvider>(), Mock.Of<ILogger>(), null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Get_CSharpProject_CSharpVBProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.xxxx");
            projectToLanguageMapper.Setup(x => x.GetAllBindingLanguagesForProject(project))
                .Returns(new[] {Language.CSharp});

            using (new AssertIgnoreScope())
            {
                var configProjectBinder = testSubject.Get(project);
                configProjectBinder.Should().BeOfType<CSharpVBProjectBinder>();
            }
        }

        [TestMethod]
        public void Get_VbNetProject_CSharpVBProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.xxxx");
            projectToLanguageMapper.Setup(x => x.GetAllBindingLanguagesForProject(project))
                .Returns(new[] { Language.VBNET });

            using (new AssertIgnoreScope())
            {
                var configProjectBinder = testSubject.Get(project);
                configProjectBinder.Should().BeOfType<CSharpVBProjectBinder>();
            }
        }

        [TestMethod]
        public void Get_CppProject_CFamilyProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.xxxx");
            projectToLanguageMapper.Setup(x => x.GetAllBindingLanguagesForProject(project))
                .Returns(new[] { Language.Cpp });

            using (new AssertIgnoreScope())
            {
                var configProjectBinder = testSubject.Get(project);
                configProjectBinder.Should().BeOfType<CFamilyProjectBinder>();
            }
        }

        [TestMethod]
        public void Get_CProject_CFamilyProjectBinderReturned()
        {
            var project = new ProjectMock("c:\\foo.xxxx");
            projectToLanguageMapper.Setup(x => x.GetAllBindingLanguagesForProject(project))
                .Returns(new[] { Language.C });

            using (new AssertIgnoreScope())
            {
                var configProjectBinder = testSubject.Get(project);
                configProjectBinder.Should().BeOfType<CFamilyProjectBinder>();
            }
        }

        [TestMethod]
        public void Get_UnknownProject_Null()
        {
            var project = new ProjectMock("c:\\foo.xxxx");
            projectToLanguageMapper.Setup(x => x.GetAllBindingLanguagesForProject(project))
                .Returns(new[] { Language.Unknown });

            using (new AssertIgnoreScope())
            {
                var configProjectBinder = testSubject.Get(project);
                configProjectBinder.Should().BeNull();
            }
        }
    }
}
