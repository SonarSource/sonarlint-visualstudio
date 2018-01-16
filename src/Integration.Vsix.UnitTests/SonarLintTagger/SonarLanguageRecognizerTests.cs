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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarLanguageRecognizerTests
    {
        private Mock<IContentTypeRegistryService> contentTypeServiceMock;
        private Mock<IFileExtensionRegistryService> fileExtensionServiceMock;
        private SonarLanguageRecognizer testSubject;
        private Mock<ITextDocument> textDocumentMock;
        private Mock<ITextBuffer> textBufferMock;

       [TestInitialize]
        public void TestInitialize()
        {
            contentTypeServiceMock = new Mock<IContentTypeRegistryService>();
            fileExtensionServiceMock = new Mock<IFileExtensionRegistryService>();
            testSubject = new SonarLanguageRecognizer(contentTypeServiceMock.Object, fileExtensionServiceMock.Object);

            textDocumentMock = new Mock<ITextDocument>();
            textBufferMock = new Mock<ITextBuffer>();
        }

        [TestMethod]
        public void Class_IsMEF()
        {
            // Arrange
            var requiredExports = new List<Export>();
            requiredExports.Add(MefTestHelpers.CreateExport<IContentTypeRegistryService>(
                new Mock<IContentTypeRegistryService>().Object));
            requiredExports.Add(MefTestHelpers.CreateExport<IFileExtensionRegistryService>(
                new Mock<IFileExtensionRegistryService>().Object));

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<SonarLanguageRecognizer, ISonarLanguageRecognizer>(Enumerable.Empty<Export>(),
                requiredExports);
        }

        [TestMethod]
        public void Ctor_WhenContentTypeRegistryServiceIsNull_Throws()
        {
            // Arrange
            Action act = () => new SonarLanguageRecognizer(null, new Mock<IFileExtensionRegistryService>().Object);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("contentTypeRegistryService");
        }

        [TestMethod]
        public void Ctor_WhenFileExtensionRegistryServiceIsNull_Throws()
        {
            // Arrange
            Action act = () => new SonarLanguageRecognizer(new Mock<IContentTypeRegistryService>().Object, null);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("fileExtensionRegistryService");
        }

        [TestMethod]
        public void Detect_WhenFileExtensionIsJavascriptRelated_ReturnsJavascript()
        {
            foreach (var expectedExtension in new[] { "js", "jsx", "vue" })
            {
                // Act
                textDocumentMock.Setup(x => x.FilePath).Returns($"foo.{expectedExtension}");
                var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

                // Assert
                result.Should().HaveCount(1);
                result.First().Should().Be(SonarLanguage.Javascript);
            }
        }

        [TestMethod]
        public void Detect_WhenExtensionNotRegisteredAndBufferContentTypeIsJsRelated_ReturnsJavascript()
        {
            // Arrange
            textDocumentMock.Setup(x => x.FilePath).Returns("foo");

            var contentTypeMock = new Mock<IContentType>();
            textBufferMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);

            foreach (var expectedType in new[] { "JavaScript", "Vue" })
            {
                contentTypeMock.Setup(x => x.IsOfType(expectedType)).Returns(true);

                // Act
                var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

                // Assert
                result.Should().HaveCount(1);
                result.First().Should().Be(SonarLanguage.Javascript);
            }
        }

        [TestMethod]
        public void Detect_WhenExtensionNotRegisteredAndBufferContentTypeIsCRelated_ReturnsCFamily()
        {
            // Arrange
            textDocumentMock.Setup(x => x.FilePath).Returns("foo");

            var contentTypeMock = new Mock<IContentType>();
            contentTypeMock.Setup(x => x.IsOfType("C/C++")).Returns(true);
            textBufferMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);

            // Act
            var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be(SonarLanguage.CFamily);
        }

        [TestMethod]
        public void Detect_WhenExtensionIsRegisteredAsJs_ReturnsJavascript()
        {
            // Arrange
            var fileExtension = "XXX";
            textDocumentMock.Setup(x => x.FilePath).Returns($"foo.{fileExtension}");
            var contentTypeMock = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock.Object });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock.Object)).Returns(new[] { fileExtension });
            contentTypeMock.Setup(x => x.IsOfType("JavaScript")).Returns(true);

            // Act
            var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be(SonarLanguage.Javascript);
        }

        [TestMethod]
        public void Detect_WhenExtensionIsRegisteredAsC_ReturnsCFamily()
        {
            // Arrange
            var fileExtension = "XXX";
            textDocumentMock.Setup(x => x.FilePath).Returns($"foo.{fileExtension}");
            var contentTypeMock = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock.Object });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock.Object)).Returns(new[] { fileExtension });
            contentTypeMock.Setup(x => x.IsOfType("C/C++")).Returns(true);

            // Act
            var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be(SonarLanguage.CFamily);
        }

        [TestMethod]
        public void Detect_WhenExtensionIsRegisteredAsCandJs_ReturnsCFamilyAndJavascript()
        {
            // Arrange
            var fileExtension = "XXX";
            textDocumentMock.Setup(x => x.FilePath).Returns($"foo.{fileExtension}");
            var contentTypeMock1 = new Mock<IContentType>();
            var contentTypeMock2 = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock1.Object, contentTypeMock2.Object });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock1.Object)).Returns(new[] { fileExtension });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock2.Object)).Returns(new[] { fileExtension });
            contentTypeMock1.Setup(x => x.IsOfType("C/C++")).Returns(true);
            contentTypeMock2.Setup(x => x.IsOfType("JavaScript")).Returns(true);

            // Act
            var result = testSubject.Detect(textDocumentMock.Object, textBufferMock.Object);

            // Assert
            result.Should().HaveCount(2);
            result.First().Should().Be(SonarLanguage.Javascript);
            result.Skip(1).First().Should().Be(SonarLanguage.CFamily);
        }
    }
}
