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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ErrorTaggerProvider, ITaggerProvider>(null,
                new[]
                {
                    MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(Mock.Of<IBufferTagAggregatorFactoryService>()),
                    MefTestHelpers.CreateExport<IErrorTagTooltipProvider>(Mock.Of<IErrorTagTooltipProvider>())
                });
        }

        [TestMethod]
        public void CreateTagger_BufferIsNull_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.CreateTagger<ITag>(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestMethod]
        public void CreateTagger_BufferIsNotNull_ReturnsNewTaggerPerRequest()
        {
            var buffer = CreateBuffer();
            var testSubject = CreateTestSubject();

            // 1. Request first tagger for buffer
            var tagger1 = testSubject.CreateTagger<ITag>(buffer);
            tagger1.Should().NotBeNull();

            // 2. Request second tagger - expecting a different instance
            var tagger2 = testSubject.CreateTagger<ITag>(buffer);
            tagger2.Should().NotBeNull();

            tagger1.Should().NotBeSameAs(tagger2);
        }

        [TestMethod]
        public void CreateTagger_TwoBuffers_ReturnsNewTaggerPerRequest()
        {
            var buffer1 = CreateBuffer();
            var buffer2 = CreateBuffer();
            var testSubject = CreateTestSubject();

            // 1. Request tagger for first buffer
            var tagger1 = testSubject.CreateTagger<ITag>(buffer1);
            tagger1.Should().NotBeNull();

            // 2. Request tagger for second buffer - expecting a different instance
            var tagger2 = testSubject.CreateTagger<ITag>(buffer2);
            tagger2.Should().NotBeNull();

            tagger1.Should().NotBeSameAs(tagger2);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void CreateTagger_NoFilePath_NullBuffer(string bufferFilePath)
        {
            var buffer = CreateBufferMock(filePath: bufferFilePath);
            var testSubject = CreateTestSubject();

            var tagger = testSubject.CreateTagger<ITag>(buffer.Object);
            tagger.Should().BeNull();
        }

        private ErrorTaggerProvider CreateTestSubject()
        {
            var aggregatorMock = new Mock<IBufferTagAggregatorFactoryService>();
            aggregatorMock.Setup(x => x.CreateTagAggregator<IIssueLocationTag>(It.IsAny<ITextBuffer>()))
                .Returns(Mock.Of<ITagAggregator<IIssueLocationTag>>());

            return new ErrorTaggerProvider(aggregatorMock.Object,Mock.Of<IErrorTagTooltipProvider>());
        }
    }
}
