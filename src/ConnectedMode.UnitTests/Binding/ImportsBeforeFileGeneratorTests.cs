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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class ImportsBeforeFileGeneratorTests
    {
        [TestMethod]
        public void FileDoesNotExist_CreatesFile()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");
            string expectedResult = "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n  <ItemGroup />\r\n</Project>";

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(false);

            _ = new ImportBeforeFileGenerator(fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, expectedResult), Times.Once);
        }

        [TestMethod]
        public void FileExists_DifferentText_CreatesFile()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");
            string expectedResult = "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n  <ItemGroup />\r\n</Project>";
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(true);
            fileSystem.Setup(x => x.File.ReadAllText(pathToFile)).Returns("wrong text");

            _ = new ImportBeforeFileGenerator(fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, expectedResult), Times.Once);
        }

        [TestMethod]
        public void FileExists_SameText_DoesNotCreateFile()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");
            string expectedResult = "<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n  <ItemGroup />\r\n</Project>";

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(true);
            fileSystem.Setup(x => x.File.ReadAllText(pathToFile)).Returns(expectedResult);

            _ = new ImportBeforeFileGenerator(fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, expectedResult), Times.Never);
        }

        [TestMethod]
        public void DirectoryDoesNotExist_CreatesDirectory()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(false);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(false);

            _ = new ImportBeforeFileGenerator(fileSystem.Object);

            fileSystem.Verify(x => x.Directory.CreateDirectory(pathToDirectory), Times.Once);
        }

        private string GetPathToImportBefore()
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var pathToImportsBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

            return pathToImportsBefore;
        }
    }
}
