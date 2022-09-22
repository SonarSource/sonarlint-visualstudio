/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class TaggableBufferIndicatorTests
    {
        private const string FilePath = "test.cpp";

        [TestMethod]
        public void IsTaggable_NoFilePath_False()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: null);

            using (new AssertIgnoreScope())
            {
                var fileSystem = new Mock<IFileSystem>();
                var testSubject = new TaggableBufferIndicator(fileSystem.Object);

                var result = testSubject.IsTaggable(buffer.Object);
                result.Should().BeFalse();

                fileSystem.Invocations.Count.Should().Be(0);
            }
        }

        [TestMethod]
        public void IsTaggable_FileDoesNotExist_False()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: FilePath);
            var testSubject = CreateTestSubject(fileExists: false);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_FileExists_True()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: FilePath);
            var testSubject = CreateTestSubject(fileExists: true);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeTrue();
        }

        private TaggableBufferIndicator CreateTestSubject(bool fileExists)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(FilePath)).Returns(fileExists);

            return new TaggableBufferIndicator(fileSystem.Object);
        }
    }
}
