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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common
{
    /// <summary>
    /// Common tests that apply to our View TaggerProviders
    /// </summary>
    [TestClass]
    public abstract class CommonViewTaggerProviderTestsBase
    {
        internal abstract IViewTaggerProvider CreateTestSubject(ITaggableBufferIndicator taggableBufferIndicator);

        private IViewTaggerProvider CreateTestSubject() => CreateTestSubject(CreateTaggableBufferIndicator());

        [TestMethod]
        public void CreateTagger_ViewIsNull_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.CreateTagger<ITag>(null, ValidBuffer);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("textView");
        }

        [TestMethod]
        public void CreateTagger_BufferIsNull_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.CreateTagger<ITag>(Mock.Of<ITextView>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestMethod]
        public void CreateTagger_SuppliedBufferDoesNotMatchTextViewBuffer_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            var suppliedBuffer = ValidBuffer;

            var viewBuffer = CreateBuffer();
            var view = CreateValidTextView(viewBuffer);

            var tagger = testSubject.CreateTagger<ITag>(view, suppliedBuffer);
            tagger.Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_BufferIsNotTaggable_Null()
        {
            var textBuffer = ValidBuffer;
            var textView = CreateValidTextView(textBuffer);
            var taggableBufferIndicator = CreateTaggableBufferIndicator(isTaggable: false);

            var testSubject = CreateTestSubject(taggableBufferIndicator);
            var tagger = testSubject.CreateTagger<ITag>(textView, textBuffer);

            tagger.Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_BufferMatchesTextView_ReturnsSingletonTaggerPerView()
        {
            var testSubject = CreateTestSubject();

            var view = CreateValidTextView(ValidBuffer);

            // 1. Request first tagger for view/buffer
            var tagger1 = testSubject.CreateTagger<ITag>(view, view.TextBuffer);
            tagger1.Should().NotBeNull();

            // 2. Request second tagger - expecting the same instance
            var tagger2 = testSubject.CreateTagger<ITag>(view, view.TextBuffer);
            tagger2.Should().NotBeNull();

            tagger1.Should().BeSameAs(tagger2);
        }

        [TestMethod]
        public void CreateTagger_SameBufferDifferentViews_ReturnsTaggerPerView()
        {
            var view1 = CreateValidTextView(ValidBuffer);
            var view2 = CreateValidTextView(ValidBuffer);

            var testSubject = CreateTestSubject();

            // 1. Request tagger for first view
            var tagger1 = testSubject.CreateTagger<ITag>(view1, ValidBuffer);
            tagger1.Should().NotBeNull();

            // 2. Request tagger for second view - expecting a different instance
            var tagger2 = testSubject.CreateTagger<ITag>(view2, ValidBuffer);
            tagger2.Should().NotBeNull();

            tagger1.Should().NotBeSameAs(tagger2);
        }

        [TestMethod]
        public void CreateTagger_DifferentBuffers_ReturnsTaggerPerViewBuffer()
        {
            var buffer1 = CreateBuffer();
            var buffer2 = CreateBuffer();

            var view1 = CreateValidTextView(buffer1);
            var view2 = CreateValidTextView(buffer2);

            var testSubject = CreateTestSubject();

            // 1. Request tagger for first view/buffer
            var tagger1 = testSubject.CreateTagger<ITag>(view1, buffer1);
            tagger1.Should().NotBeNull();

            // 2. Request tagger for second view/buffer - expecting a different instance
            var tagger2 = testSubject.CreateTagger<ITag>(view2, buffer2);
            tagger2.Should().NotBeNull();

            tagger1.Should().NotBeSameAs(tagger2);
        }

        private static ITextView CreateValidTextView(ITextBuffer buffer = null)
        {
            var viewMock = new Mock<ITextView>();
            var properties = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            viewMock.Setup(x => x.Properties).Returns(properties);
            viewMock.Setup(x => x.TextBuffer).Returns(buffer);

            viewMock.As<IWpfTextView>();

            return viewMock.Object;
        }
    }
}
