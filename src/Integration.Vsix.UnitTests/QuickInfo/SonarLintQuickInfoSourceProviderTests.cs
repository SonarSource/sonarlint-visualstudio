/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.QuickInfo
{
    [TestClass]
    public class SonarLintQuickInfoSourceProviderTests
    {
        [TestMethod]
        public void TryCreateQuickInfoSource_Returns_New_Instance()
        {
            var textBufferMock = new Mock<ITextBuffer>(MockBehavior.Strict);
            textBufferMock.SetupGet(x => x.CurrentSnapshot).Returns(new Mock<ITextSnapshot>().Object);

            var textDocumentMock = new Mock<ITextDocument>();
            textDocumentMock.Setup(x => x.FilePath).Returns("some path");
            var textDocument = textDocumentMock.Object;

            var textDocumentFactoryServiceMock = new Mock<ITextDocumentFactoryService>();
            textDocumentFactoryServiceMock
                .Setup(x => x.TryGetTextDocument(textBufferMock.Object, out textDocument))
                .Returns(true);

            var provider = new SonarLintQuickInfoSourceProvider
            {
                TextDocumentFactoryService = textDocumentFactoryServiceMock.Object
            };

            // Act
            var result = provider.TryCreateQuickInfoSource(textBufferMock.Object);

            // Assert
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void TryCreateQuickInfoSource_Returns_Null()
        {
            var textBufferMock = new Mock<ITextBuffer>(MockBehavior.Strict);
            textBufferMock.SetupGet(x => x.CurrentSnapshot).Returns(new Mock<ITextSnapshot>().Object);

            ITextDocument textDocument = null;

            var textDocumentFactoryServiceMock = new Mock<ITextDocumentFactoryService>();
            textDocumentFactoryServiceMock
                .Setup(x => x.TryGetTextDocument(textBufferMock.Object, out textDocument))
                .Returns(false);

            var provider = new SonarLintQuickInfoSourceProvider
            {
                TextDocumentFactoryService = textDocumentFactoryServiceMock.Object
            };

            // Act
            var result = provider.TryCreateQuickInfoSource(textBufferMock.Object);

            // Assert
            result.Should().BeNull();
        }
    }
}
