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
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    /// <summary>
    /// Common tests that apply to our Buffer TaggerProviders
    /// </summary>
    public abstract class CommonTaggerProviderTestsBase
    {
        internal abstract ITaggerProvider CreateTestSubject(ITaggableBufferIndicator taggableBufferIndicator);

        private ITaggerProvider CreateTestSubject() => CreateTestSubject(CreateTaggableBufferIndicator());

        [TestMethod]
        public void CreateTagger_BufferIsNull_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.CreateTagger<ITag>(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestMethod]
        public void CreateTagger_BufferIsNotNull_ReturnsSingletonTagger()
        {
            var buffer = CreateBuffer();
            var testSubject = CreateTestSubject();

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
        public void CreateTagger_BufferIsNotTaggable_Null()
        {
            var textBuffer = CreateBuffer();
            var taggableBufferIndicator = CreateTaggableBufferIndicator(isTaggable: false);

            var testSubject = CreateTestSubject(taggableBufferIndicator);
            var tagger = testSubject.CreateTagger<ITag>(textBuffer);

            tagger.Should().BeNull();
        }
    }
}
