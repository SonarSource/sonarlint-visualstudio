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
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;
using TypedSingleton = SonarLint.VisualStudio.IssueVisualization.Editor.SingletonDisposableTaggerManager<SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SingletonDisposableTaggerManager_FlyweightTests.IDummyTagType>;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class SingletonDisposableTaggerManager_FlyweightTests
    {
        private static ITagger<IDummyTagType> ValidSingleton = CreateSingletonTaggerMock().Object;
        private static TypedSingleton.OnTaggerDisposed ValidOnDisposedDelegate = CreateOnDisposedMock().Object;

        public interface IDummyTagType : ITag { }

        [TestMethod]
        public void Dispose_DisposeDelegateCalled()
        {
            var onDisposedMock = CreateOnDisposedMock();
            var testSubject = CreateTestSubject(ValidSingleton, onDisposedMock.Object);

            onDisposedMock.Invocations.Count.Should().Be(0);

            // 1. Dispose once -> delegate called
            testSubject.Dispose();
            onDisposedMock.Invocations.Count.Should().Be(1);

            // 2. Dispose again -> delegate not called
            testSubject.Dispose();
            onDisposedMock.Invocations.Count.Should().Be(1);
        }

        [TestMethod]
        public void GetTags_ForwardsCall()
        {
            var singletonMock = CreateSingletonTaggerMock();
            var inputSpans = new NormalizedSnapshotSpanCollection();
            var expectedTags = new[] { CreateNewTagSpan(), CreateNewTagSpan() };
            singletonMock.Setup(x => x.GetTags(inputSpans)).Returns(expectedTags);

            var testSubject = CreateTestSubject(singletonMock.Object, ValidOnDisposedDelegate);

            var actualTags = testSubject.GetTags(inputSpans);

            singletonMock.Verify(x => x.GetTags(inputSpans), Times.Once);
            actualTags.Should().BeEquivalentTo(expectedTags);
        }

        [TestMethod]
        public void TagsChanged_ForwardsEventRegistration()
        {
            var singletonMock = CreateSingletonTaggerMock();
            singletonMock.SetupAdd(x => x.TagsChanged += null);
            var testSubject = CreateTestSubject(singletonMock.Object, ValidOnDisposedDelegate);

            singletonMock.VerifyAdd(x => x.TagsChanged += It.IsAny<EventHandler<SnapshotSpanEventArgs>>(), Times.Never);

            testSubject.TagsChanged += (sender, args) => { };

            singletonMock.VerifyAdd(x => x.TagsChanged += It.IsAny<EventHandler<SnapshotSpanEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void TagsChanged_ForwardsEventUnegistration()
        {
            var singletonMock = CreateSingletonTaggerMock();
            singletonMock.SetupRemove(x => x.TagsChanged -= null);
            var testSubject = CreateTestSubject(singletonMock.Object, ValidOnDisposedDelegate);

            singletonMock.VerifyRemove(x => x.TagsChanged -= It.IsAny<EventHandler<SnapshotSpanEventArgs>>(), Times.Never);

            testSubject.TagsChanged -= (sender, args) => { } ;

            singletonMock.VerifyRemove(x => x.TagsChanged -= It.IsAny<EventHandler<SnapshotSpanEventArgs>>(), Times.Once);
        }

        private TypedSingleton.FlyweightTaggerWrapper CreateTestSubject(ITagger<IDummyTagType> singleton, TypedSingleton.OnTaggerDisposed onTaggerDisposed) =>
            new TypedSingleton.FlyweightTaggerWrapper(singleton, onTaggerDisposed);

        private static Mock<ITagger<IDummyTagType>> CreateSingletonTaggerMock() =>
            new Mock<ITagger<IDummyTagType>>();

        private static Mock<TypedSingleton.OnTaggerDisposed> CreateOnDisposedMock() =>
            new Mock<TypedSingleton.OnTaggerDisposed>();

        private static ITagSpan<IDummyTagType> CreateNewTagSpan()
        {
            var snapshot = CreateSnapshot(length: 10);
            var snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(1, 2));
            return new TagSpan<IDummyTagType>(snapshotSpan, Mock.Of<IDummyTagType>());
        }
    }
}
