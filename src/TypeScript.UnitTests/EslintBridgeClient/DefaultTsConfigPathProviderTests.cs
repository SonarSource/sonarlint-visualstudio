/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class DefaultTsConfigPathProviderTests
    {
        [TestMethod]
        public void GetFilePath_FileDoesNotExist_FileIsCreated()
        {
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory(Path.GetDirectoryName(DefaultTsConfigPathProvider.DefaultTsConfigFilePath));

            var testSubject = CreateTestSubject(fileSystem);

            var result = testSubject.GetFilePath();
            result.Should().Be(DefaultTsConfigPathProvider.DefaultTsConfigFilePath);

            var mockFileData = fileSystem.GetFile(result);
            mockFileData.Should().NotBeNull();
            mockFileData.TextContents.Should().BeEquivalentTo(@"{
  ""files"": [],
  ""compilerOptions"": {
    ""allowJs"": true,
    ""noImplicitAny"": true
  }
}");
        }

        [TestMethod]
        public void GetFilePath_FileExists_FileIsNotReCreated()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(DefaultTsConfigPathProvider.DefaultTsConfigFilePath)).Returns(true);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.GetFilePath();
            result.Should().Be(DefaultTsConfigPathProvider.DefaultTsConfigFilePath);

            fileSystem.VerifyAll();
            fileSystem.VerifyNoOtherCalls();
        }

        private DefaultTsConfigPathProvider CreateTestSubject(IFileSystem fileSystem)
        {
            return new DefaultTsConfigPathProvider(fileSystem);
        }
    }
}
