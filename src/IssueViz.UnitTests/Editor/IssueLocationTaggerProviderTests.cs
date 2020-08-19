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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.BufferTagger;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.BufferTagger
{
    [TestClass]
    public class IssueLocationTaggerProviderTests
    {
        private readonly IAnalysisIssueSelectionService ValidSelectionService = Mock.Of<IAnalysisIssueSelectionService>();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var selectionServiceExport = MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(ValidSelectionService);

            MefTestHelpers.CheckTypeCanBeImported<IssueLocationTaggerProvider, ITaggerProvider>(null, new[] { selectionServiceExport });
        }

        [TestMethod]
        public void CreateTagger_BufferIsNull_Throws()
        {
            var testSubject = (ITaggerProvider)new IssueLocationTaggerProvider(ValidSelectionService);

            Action act = () => testSubject.CreateTagger<ITag>(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestMethod]
        public void CreateTagger_BufferIsNotNull_ReturnsSingletonTagger()
        {
            var buffer = CreateValidTextBuffer();
            var testSubject = (ITaggerProvider)new IssueLocationTaggerProvider(ValidSelectionService);

            // 1. Request first tagger for buffer
            var tagger1 = testSubject.CreateTagger<ITag>(buffer);
            tagger1.Should().NotBeNull();

            // 2. Request second tagger - expecting the same instance
            var tagger2 = testSubject.CreateTagger<ITag>(buffer);
            tagger2.Should().NotBeNull();

            tagger1.Should().BeSameAs(tagger2);
        }

        [TestMethod]
        public void CreateTagger_TwoBuffers_DifferentTaggerPerBuffer()
        {
            var buffer1 = CreateValidTextBuffer();
            var buffer2 = CreateValidTextBuffer();
            var testSubject = (ITaggerProvider)new IssueLocationTaggerProvider(ValidSelectionService);

            // 1. Request tagger for first buffer
            var tagger1 = testSubject.CreateTagger<ITag>(buffer1);
            tagger1.Should().NotBeNull();

            // 2. Request tagger for second buffer - expecting a different instance
            var tagger2 = testSubject.CreateTagger<ITag>(buffer2);
            tagger2.Should().NotBeNull();

            tagger1.Should().NotBeSameAs(tagger2);
        }

        private static ITextBuffer CreateValidTextBuffer()
        {
            var mockBuffer = new Mock<ITextBuffer>();
            var properties = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            mockBuffer.Setup(x => x.Properties).Returns(properties);
            return mockBuffer.Object;
        }
    }
}
