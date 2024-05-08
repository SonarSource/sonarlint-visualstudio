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

using System.IO;
using System.IO.Abstractions;
using Microsoft.VisualStudio.Text.Projection;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class TaggableBufferIndicatorTests
    {
        [TestMethod]
        public void IsTaggable_ProjectionBuffer_False()
        {
            var projectionBufferMock = new Mock<IProjectionBuffer>();
            var testSubject = CreateTestSubject("a", true);

            testSubject.IsTaggable(projectionBufferMock.Object).Should().BeFalse();
        }

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
            const string filePath = "test.cpp";
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: filePath);
            var testSubject = CreateTestSubject(filePath, fileExists: false);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_FileExists_InTemp_False()
        {
            var filePath = Path.GetTempFileName().ToUpper(); // should ignore casing
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: filePath);
            var testSubject = CreateTestSubject(filePath, fileExists: true);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_FileExists_InTempSubFolder_False()
        {
            var directory = new DirectoryInfo(Path.GetTempPath()).FullName;
            var filePath = Path.Combine(directory, "sub\\test.cpp");
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: filePath);
            var testSubject = CreateTestSubject(filePath, fileExists: true);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_FileExists_NotInTemp_True()
        {
            var directory = new DirectoryInfo(Path.GetTempPath()).Parent.FullName;
            var filePath = Path.Combine(directory, "test.cpp");

            var buffer = TaggerTestHelper.CreateBufferMock(filePath: filePath);
            var testSubject = CreateTestSubject(filePath, fileExists: true);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeTrue();
        }

        private TaggableBufferIndicator CreateTestSubject(string filePath, bool fileExists)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(filePath)).Returns(fileExists);

            return new TaggableBufferIndicator(fileSystem.Object);
        }
    }
}
