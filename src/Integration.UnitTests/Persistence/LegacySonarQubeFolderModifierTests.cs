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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacySonarQubeFolderModifierTests
    {
        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new LegacySonarQubeFolderModifier(null, Mock.Of<IFileSystem>());

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new LegacySonarQubeFolderModifier(Mock.Of<IServiceProvider>(), null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Add_AddsFileToSolutionFolder()
        {
            var projectMock = Mock.Of<EnvDTE.Project>();
            var projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            projectSystemHelperMock
                .Setup(x => x.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true))
                .Returns(projectMock);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.Exists("c:\\test")).Returns(true);

            var testSubject = new LegacySonarQubeFolderModifier(serviceProviderMock.Object, fileSystemMock.Object);
            testSubject.Add("c:\\test");

            projectSystemHelperMock.Verify(x=> x.AddFileToProject(projectMock, "c:\\test"), Times.Once);
        }
    }
}
