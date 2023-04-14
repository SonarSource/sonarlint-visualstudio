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
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LanguageDetection
{
    [TestClass]
    public class SonarLanguageRecognizerTests
    {
        private Mock<IContentTypeRegistryService> contentTypeServiceMock;
        private Mock<IFileExtensionRegistryService> fileExtensionServiceMock;
        private SonarLanguageRecognizer testSubject;

        private Mock<IContentType> CFamilyType = new Mock<IContentType>();
        private Mock<IContentType> TypeScriptType = new Mock<IContentType>();
        private Mock<IContentType> CSharpType = new Mock<IContentType>();
        private Mock<IContentType> BasicType = new Mock<IContentType>();
        private Mock<IContentType> UnknownType = new Mock<IContentType>();
        private Mock<IContentType> CSSType = new Mock<IContentType>();
        private Mock<IContentType> SCSSType = new Mock<IContentType>();
        private Mock<IContentType> LESSType = new Mock<IContentType>();

        [TestInitialize]
        public void TestInitialize()
        {
            contentTypeServiceMock = new Mock<IContentTypeRegistryService>();
            fileExtensionServiceMock = new Mock<IFileExtensionRegistryService>();
            testSubject = new SonarLanguageRecognizer(contentTypeServiceMock.Object, fileExtensionServiceMock.Object);
            FileExtensionServiceSetup();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SonarLanguageRecognizer, ISonarLanguageRecognizer>(
                MefTestHelpers.CreateExport<IContentTypeRegistryService>(),
                MefTestHelpers.CreateExport<IFileExtensionRegistryService>());
        }

        [TestMethod]
        public void Ctor_WhenContentTypeRegistryServiceIsNull_Throws()
        {
            // Arrange
            Action act = () => new SonarLanguageRecognizer(null, new Mock<IFileExtensionRegistryService>().Object);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("contentTypeRegistryService");
        }

        [TestMethod]
        public void Ctor_WhenFileExtensionRegistryServiceIsNull_Throws()
        {
            // Arrange
            Action act = () => new SonarLanguageRecognizer(new Mock<IContentTypeRegistryService>().Object, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileExtensionRegistryService");
        }

        [TestMethod]
        public void Detect_WhenFileExtensionIsJavascriptRelated_ReturnsJavascript()
        {
            foreach (var expectedExtension in new[] { "js", "jsx", "vue" })
            {
                // Act
                var result = testSubject.Detect($"foo.{expectedExtension}", null);

                // Assert
                result.Should().HaveCount(1);
                result.First().Should().Be(AnalysisLanguage.Javascript);
            }
        }

        [TestMethod]
        public void Detect_WhenExtensionIsUnknownAndBufferIsUnknown_ReturnsEmptyList()
        {
            // Arrange
            var contentTypeMock = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock.Object });

            // Act
            var result = testSubject.Detect("foo", null);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("JavaScript", AnalysisLanguage.Javascript)]
        [DataRow("Vue", AnalysisLanguage.Javascript)]
        [DataRow("C/C++", AnalysisLanguage.CFamily)]
        [DataRow("Roslyn Languages", AnalysisLanguage.RoslynFamily)]
        [DataRow("TypeScript", AnalysisLanguage.TypeScript)]
        [DataRow("css", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("SCSS", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("LESS", AnalysisLanguage.CascadingStyleSheets)]
        public void Detect_WhenExtensionNotRegistered_ReturnsLanguageFromBufferContentType(string bufferContentType, AnalysisLanguage expectedLanguage)
        {
            // Arrange
            var contentTypeMock = new Mock<IContentType>();
            contentTypeMock.Setup(x => x.IsOfType(bufferContentType)).Returns(true);

            // Act
            var result = testSubject.Detect("foo", contentTypeMock.Object);

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be(expectedLanguage);
        }

        [TestMethod]
        [DataRow("JavaScript", AnalysisLanguage.Javascript)]
        [DataRow("Vue", AnalysisLanguage.Javascript)]
        [DataRow("C/C++", AnalysisLanguage.CFamily)]
        [DataRow("Roslyn Languages", AnalysisLanguage.RoslynFamily)]
        [DataRow("TypeScript", AnalysisLanguage.TypeScript)]
        [DataRow("css", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("SCSS", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("LESS", AnalysisLanguage.CascadingStyleSheets)]
        public void Detect_WhenExtensionIsRegistered_ReturnsLanguageFromExtension(string bufferContentType, AnalysisLanguage expectedLanguage)
        {
            // Arrange
            var fileExtension = "XXX";
            var contentTypeMock = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock.Object });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock.Object)).Returns(new[] { fileExtension });
            contentTypeMock.Setup(x => x.IsOfType(bufferContentType)).Returns(true);

            // Act
            var result = testSubject.Detect($"foo.{fileExtension}", null);

            // Assert
            result.Should().HaveCount(1);
            result.First().Should().Be(expectedLanguage);
        }

        [TestMethod]
        public void Detect_WhenExtensionIsRegisteredAsMultipleTypes_ReturnsMultipleLanguages()
        {
            // Arrange
            var fileExtension = "XXX";
            var contentTypeMock1 = new Mock<IContentType>();
            var contentTypeMock2 = new Mock<IContentType>();
            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentTypeMock1.Object, contentTypeMock2.Object });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock1.Object)).Returns(new[] { fileExtension });
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentTypeMock2.Object)).Returns(new[] { fileExtension });
            contentTypeMock1.Setup(x => x.IsOfType("C/C++")).Returns(true);
            contentTypeMock2.Setup(x => x.IsOfType("JavaScript")).Returns(true);

            // Act
            var result = testSubject.Detect($"foo.{fileExtension}", null);

            // Assert
            result.Should().HaveCount(2);
            result.First().Should().Be(AnalysisLanguage.Javascript);
            result.Skip(1).First().Should().Be(AnalysisLanguage.CFamily);
        }

        [TestMethod]
        public void Detect_WhenContentTypeIsTypeScriptButFileExtensionIsJavaScript_ReturnsJavaScript()
        {
            var jsFileExtension = "js";
            var contentType = new Mock<IContentType>();

            contentTypeServiceMock.Setup(x => x.ContentTypes).Returns(new[] { contentType.Object });
            contentType.Setup(x => x.IsOfType("TypeScript")).Returns(true);
            fileExtensionServiceMock.Setup(x => x.GetExtensionsForContentType(contentType.Object)).Returns(new[] { jsFileExtension });

            var result = testSubject.Detect($"foo.{jsFileExtension}", null);

            result.Should().HaveCount(1);
            result.First().Should().Be(AnalysisLanguage.Javascript);
        }

        [DataRow("File.cs", AnalysisLanguage.RoslynFamily)]
        [DataRow("File.CS", AnalysisLanguage.RoslynFamily)]
        [DataRow("File.vb", AnalysisLanguage.RoslynFamily)]
        [DataRow("File.js", AnalysisLanguage.Javascript)]
        [DataRow("File.ts", AnalysisLanguage.TypeScript)]
        [DataRow("File.cpp", AnalysisLanguage.CFamily)]
        [DataRow("File.css", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("File.scss", AnalysisLanguage.CascadingStyleSheets)]
        [DataRow("File.less", AnalysisLanguage.CascadingStyleSheets)]
        [TestMethod]
        public void GetAnalysisLanguageFromExtension_ReturnsAnalysisLangFromExtension(string fileName, AnalysisLanguage expectedLanguage)
        {
            var actualLanguage = testSubject.GetAnalysisLanguageFromExtension(fileName);

            actualLanguage.Should().NotBeNull();
            actualLanguage.Value.Should().Be(expectedLanguage);
        }

        [DataRow("File.json")]
        [DataRow("Folder")]
        [DataRow("<SharedProject>")] //Shared projects in .Net Framework are represented by <>
        [TestMethod]
        public void GetAnalysisLanguageFromExtension_UnknownExtensionPassed_ReturnsNull(string fileName)
        {
            var actualLanguage = testSubject.GetAnalysisLanguageFromExtension(fileName);

            actualLanguage.Should().BeNull();
        }


        private void FileExtensionServiceSetup()
        {
            ContentTypesSetup();
            GetContentTypeForExtensionSetup();
        }

        private void GetContentTypeForExtensionSetup()
        {
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension(It.IsAny<string>())).Returns(UnknownType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("js")).Returns(TypeScriptType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("ts")).Returns(TypeScriptType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("cs")).Returns(CSharpType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("vb")).Returns(BasicType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("cpp")).Returns(CFamilyType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("css")).Returns(CSSType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("scss")).Returns(SCSSType.Object);
            fileExtensionServiceMock.Setup(f => f.GetContentTypeForExtension("less")).Returns(LESSType.Object);
        }

        private void ContentTypesSetup()
        {
            CFamilyType.SetupGet(ct => ct.TypeName).Returns("C/C++");
            TypeScriptType.SetupGet(ct => ct.TypeName).Returns("TypeScript");
            CSharpType.SetupGet(ct => ct.TypeName).Returns("CSharp");
            BasicType.SetupGet(ct => ct.TypeName).Returns("Basic");
            UnknownType.SetupGet(ct => ct.TypeName).Returns("UNKNOWN");
            CSSType.SetupGet(ct => ct.TypeName).Returns("css");
            SCSSType.SetupGet(ct => ct.TypeName).Returns("SCSS");
            LESSType.SetupGet(ct => ct.TypeName).Returns("LESS");
        }
    }
}
