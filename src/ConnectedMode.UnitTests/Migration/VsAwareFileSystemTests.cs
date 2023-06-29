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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class VsAwareFileSystemTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsAwareFileSystem, IVsAwareFileSystem>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<VsAwareFileSystem>();

        [TestMethod]
        public async Task LoadAsText_CallsFileSystem()
        {
            const string fileName = "X:\\dir\\myproj.csproj";
            const string fileContents = "some content";

            var testSubject = CreateTestSubject(out var fileSystem);
            fileSystem.Setup(x => x.File.ReadAllText(fileName)).Returns(fileContents);

            var actual = await testSubject.LoadAsTextAsync(fileName);

            actual.Should().Be(fileContents);
        }

        [TestMethod]
        public async Task Save_CallsFileSystem()
        {
            const string fileName = "c:\\aaa\\foo.txt";
            const string fileContents = "some data";

            var testSubject = CreateTestSubject(out var fileSystem);

            await testSubject.SaveAsync(fileName, fileContents);

            fileSystem.Verify(x => x.File.WriteAllText(fileName, fileContents), Times.Once);
        }

        [TestMethod]
        public async Task DeleteDirectory_CallsFileSystem()
        {
            const string dirName = "c:\\aaa\\bbb";

            var testSubject = CreateTestSubject(out var fileSystem);

            await testSubject.DeleteFolderAsync(dirName);

            fileSystem.Verify(x => x.Directory.Delete(dirName, true), Times.Once);
        }

        private static VsAwareFileSystem CreateTestSubject(
            out Mock<IFileSystem> fileSystem,
            IServiceProvider serviceProvider = null,
            IThreadHandling threadHandling = null)
        {
            var logger = new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();
            serviceProvider ??= Mock.Of<IServiceProvider>();

            fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File).Returns(new Mock<IFile>().Object);
            fileSystem.Setup(x => x.Directory).Returns(new Mock<IDirectory>().Object);
            return new VsAwareFileSystem(serviceProvider, logger, threadHandling, fileSystem.Object);
        }
    }
}
