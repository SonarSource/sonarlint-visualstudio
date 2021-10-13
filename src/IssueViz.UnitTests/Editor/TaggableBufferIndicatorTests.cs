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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Utilities;
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

        private TaggableBufferIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new TaggableBufferIndicator();
        }

        [TestMethod]
        public void IsTaggable_NoFilePath_False()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: null);

            using (new AssertIgnoreScope())
            {
                var result = testSubject.IsTaggable(buffer.Object);
                result.Should().BeFalse();
            }
        }

        [TestMethod]
        public void IsTaggable_ContentTypeIsOutput_False()
        {
            var contentType = new Mock<IContentType>();
            contentType.Setup(x => x.IsOfType("Output")).Returns(true);

            var buffer = TaggerTestHelper.CreateBufferMock(filePath: FilePath);
            buffer.Setup(x => x.ContentType).Returns(contentType.Object);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_ContentTypeIsNotOutput_True()
        {
            var contentType = new Mock<IContentType>();
            contentType.Setup(x => x.IsOfType("Output")).Returns(false);

            var buffer = TaggerTestHelper.CreateBufferMock(filePath: FilePath);
            buffer.Setup(x => x.ContentType).Returns(contentType.Object);

            var result = testSubject.IsTaggable(buffer.Object);
            result.Should().BeTrue();
        }
    }
}
