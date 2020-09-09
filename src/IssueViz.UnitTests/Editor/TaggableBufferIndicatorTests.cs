/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class TaggableBufferIndicatorTests
    {
        private Mock<ISonarLanguageRecognizer> languageRecognizerMock;

        private TaggableBufferIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            languageRecognizerMock = new Mock<ISonarLanguageRecognizer>();

            testSubject = new TaggableBufferIndicator(languageRecognizerMock.Object);
        }

        [TestMethod]
        public void IsTaggable_NoDetectedLanguages_False()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: "test.cpp");

            SetupDetectedLanguages(buffer, Enumerable.Empty<AnalysisLanguage>());

            var result = testSubject.IsTaggable(buffer.Object);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTaggable_HasDetectedLanguage_True()
        {
            var buffer = TaggerTestHelper.CreateBufferMock(filePath: "test.cpp");

            SetupDetectedLanguages(buffer, new List<AnalysisLanguage> {AnalysisLanguage.Javascript});

            var result = testSubject.IsTaggable(buffer.Object);

            result.Should().BeTrue();
        }

        private void SetupDetectedLanguages(Mock<ITextBuffer> buffer, IEnumerable<AnalysisLanguage> detectedLanguages)
        {
            languageRecognizerMock
                .Setup(x => x.Detect("test.cpp", buffer.Object.ContentType))
                .Returns(detectedLanguages);
        }
    }
}
