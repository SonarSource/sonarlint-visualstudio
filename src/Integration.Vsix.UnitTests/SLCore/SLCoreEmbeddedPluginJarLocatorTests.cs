/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.IO.Abstractions;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.UnitTests.SLCore
{
    [TestClass]
    public class SLCoreEmbeddedPluginJarLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreEmbeddedPluginJarLocator, ISLCoreEmbeddedPluginJarLocator>(MefTestHelpers.CreateExport<IVsixRootLocator>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreEmbeddedPluginJarLocator>();
        }

        [TestMethod]
        public void ListJarFiles_DirectoryNotExists_ReturnsEmpty()
        {
            var vsixRootLocator = CreateVsixLocator();
            var fileSystem = CreateFileSystem(false);

            var testSubject = new SLCoreEmbeddedPluginJarLocator(vsixRootLocator, fileSystem);

            var result = testSubject.ListJarFiles();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ListJarFiles_DirectoryExists_NoFiles_ReturnsEmpty()
        {
            var vsixRootLocator = CreateVsixLocator();
            var fileSystem = CreateFileSystem(true);

            var testSubject = new SLCoreEmbeddedPluginJarLocator(vsixRootLocator, fileSystem);

            var result = testSubject.ListJarFiles();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ListJarFiles_FilesExist_ReturnsFiles()
        {
            var vsixRootLocator = CreateVsixLocator();
            var fileSystem = CreateFileSystem(true, "File1", "File2", "File3", "File4");

            var testSubject = new SLCoreEmbeddedPluginJarLocator(vsixRootLocator, fileSystem);

            var result = testSubject.ListJarFiles();

            result.Should().HaveCount(4);
            result.Should().HaveElementAt(0, "File1");
            result.Should().HaveElementAt(1, "File2");
            result.Should().HaveElementAt(2, "File3");
            result.Should().HaveElementAt(3, "File4");
        }

        private IFileSystem CreateFileSystem(bool exists, params string[] files)
        {
            var directory = CreateDirectory(exists, files);

            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.Directory.Returns(directory);

            return fileSystem;
        }

        private IDirectory CreateDirectory(bool exists, params string[] files)
        {
            var directory = Substitute.For<IDirectory>();
            directory.Exists(default).ReturnsForAnyArgs(false);
            directory.Exists("C:\\VsixRoot\\DownloadedJars").Returns(exists);

            if (exists)
            {
                directory.GetFiles("C:\\VsixRoot\\DownloadedJars", "*.jar").Returns(files);
            }

            return directory;
        }

        private IVsixRootLocator CreateVsixLocator()
        {
            var vsixLocator = Substitute.For<IVsixRootLocator>();
            vsixLocator.GetVsixRoot().Returns("C:\\VsixRoot");
            return vsixLocator;
        }
    }
}
