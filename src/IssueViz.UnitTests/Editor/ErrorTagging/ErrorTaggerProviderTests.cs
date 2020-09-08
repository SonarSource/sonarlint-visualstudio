﻿/*
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging
{
    [TestClass]
    public class ErrorTaggerProviderTests : CommonTaggerProviderTestsBase
    {
        private Mock<ITaggableBufferIndicator> taggableBufferIndicator;

        [TestInitialize]
        public void TestInitialize()
        {
            taggableBufferIndicator = new Mock<ITaggableBufferIndicator>();
            taggableBufferIndicator.Setup(x => x.IsTaggable(It.IsAny<ITextBuffer>())).Returns(true);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var aggregatorExport = MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(Mock.Of<IBufferTagAggregatorFactoryService>());
            var taggableBufferIndicatorExport = MefTestHelpers.CreateExport<ITaggableBufferIndicator>(Mock.Of<ITaggableBufferIndicator>());

            MefTestHelpers.CheckTypeCanBeImported<ErrorTaggerProvider, ITaggerProvider>(null,
                new[]
                {
                    aggregatorExport,
                    taggableBufferIndicatorExport
                });
        }

        protected override ITaggerProvider CreateTestSubject()
        {
            var aggregatorMock = new Mock<IBufferTagAggregatorFactoryService>();
            aggregatorMock.Setup(x => x.CreateTagAggregator<IIssueLocationTag>(It.IsAny<ITextBuffer>()))
                .Returns(Mock.Of<ITagAggregator<IIssueLocationTag>>());

            return new ErrorTaggerProvider(aggregatorMock.Object, taggableBufferIndicator.Object);
        }

        [TestMethod]
        public void CreateTagger_BufferIsNotTaggable_Null()
        {
            var textBuffer = TaggerTestHelper.CreateBuffer();

            taggableBufferIndicator.Reset();
            taggableBufferIndicator.Setup(x => x.IsTaggable(textBuffer)).Returns(false);

            var testSubject = CreateTestSubject();
            var tagger = testSubject.CreateTagger<ITag>(textBuffer);

            tagger.Should().BeNull();
        }
    }
}
