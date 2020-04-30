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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacyConfigFolderItemAdderTests
    {
        private const string FilePath = "c:\\test";

        private Mock<IProjectSystemHelper> projectSystemHelperMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private LegacyConfigFolderItemAdder testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.Exists(FilePath)).Returns(true);

            testSubject = new LegacyConfigFolderItemAdder(serviceProviderMock.Object, fileSystemMock.Object);
        }
        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new LegacyConfigFolderItemAdder(null, Mock.Of<IFileSystem>());

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new LegacyConfigFolderItemAdder(Mock.Of<IServiceProvider>(), null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Add_FolderDoesNotExist_ItemNotAdded()
        {
            projectSystemHelperMock
                .Setup(x => x.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true))
                .Returns((Project) null);

            using (new AssertIgnoreScope())
            {
                testSubject.AddToFolder(FilePath);
            }

            projectSystemHelperMock
                .Verify(x => x.AddFileToProject(It.IsAny<Project>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                    Times.Never);
        }

        [TestMethod]
        public void Add_FolderExists_ItemAdded()
        {
            var projectMock = Mock.Of<Project>();

            projectSystemHelperMock
                .Setup(x => x.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true))
                .Returns(projectMock);

            testSubject.AddToFolder(FilePath);

            projectSystemHelperMock.Verify(x=> x.AddFileToProject(projectMock, FilePath), Times.Once);
        }
    }
}
