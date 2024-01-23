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
using Moq;

namespace SonarLint.VisualStudio.TestInfrastructure.Extensions
{
    /// <summary>
    /// Extension methods to simplify working with IFileSystem mocks
    /// </summary>
    public static class FileSystemExtensions
    {
        public static Mock<IFileSystem> SetFileDoesNotExist(this Mock<IFileSystem> fileSystem, string fullPath) =>
            fileSystem.SetFileExists(fullPath, false);

        public static Mock<IFileSystem> SetFileExists(this Mock<IFileSystem> fileSystem, string fullPath) =>
            fileSystem.SetFileExists(fullPath, true);

        public static Mock<IFileSystem> SetFileExists(this Mock<IFileSystem> fileSystem, string fullPath, bool result)
        {
            fileSystem.Setup(x => x.File.Exists(fullPath)).Returns(result);
            return fileSystem;
        }

        public static Mock<IFileSystem> SetFileReadAllText(this Mock<IFileSystem> fileSystem, string fullPath, string result)
        {
            fileSystem
                .SetFileExists(fullPath) // saying a file has contents implies it exists
                .Setup(x => x.File.ReadAllText(fullPath)).Returns(result);
            return fileSystem;
        }

        public static Mock<IFileSystem> VerifyFileExistsCalledOnce(this Mock<IFileSystem> fileSystem, string fullPath)
        {
            fileSystem.Verify(x => x.File.Exists(fullPath), Times.Once);
            return fileSystem;
        }

        public static Mock<IFileSystem> VerifyFileReadAllTextCalledOnce(this Mock<IFileSystem> fileSystem, string fullPath)
        {
            fileSystem.Verify(x => x.File.ReadAllText(fullPath), Times.Once);
            return fileSystem;
        }
    }
}
