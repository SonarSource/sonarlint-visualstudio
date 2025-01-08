/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
        public static T SetFileExists<T>(this T fileSystem, string fullPath, bool result = true)
         where T : class, IFileSystem
        {
            fileSystem.File.Exists(fullPath).Returns(result);
            return fileSystem;
        }
        public static T VerifyFileExistsCalledOnce<T>(this T fileSystem, string fullPath)
            where T : class, IFileSystem
        {
            fileSystem.File.Received().Exists(fullPath);
            return fileSystem;
        }
    }
}
