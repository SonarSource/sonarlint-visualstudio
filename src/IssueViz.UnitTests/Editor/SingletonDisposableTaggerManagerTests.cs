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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;
using TypedSingleton = SonarLint.VisualStudio.IssueVisualization.Editor.SingletonDisposableTaggerManager<SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SingletonDisposableTaggerManagerTests.IDummyTagType>;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class SingletonDisposableTaggerManagerTests
    {
        public interface IDummyTagType : ITag { }

        [TestMethod]
        public void SingletonTagger_CreatedOnFirstUse()
        {
            var factoryMock = CreateSingletonFactoryMock();

            var testSubject = CreateTestSubject(factoryMock.Object);
            testSubject.Singleton.Should().BeNull();

            testSubject.CreateTagger(ValidBuffer);

            CheckCreateFactoryCallCount(factoryMock, 1);
            testSubject.Singleton.Should().NotBeNull();
        }

        [TestMethod]
        public void SingletonTagger_MultipleRequests_ReturnsSameSingleton()
        {
            var factoryMock = CreateSingletonFactoryMock();

            var testSubject = CreateTestSubject(factoryMock.Object);

            // First request
            testSubject.CreateTagger(ValidBuffer);
            var originalSingleton = testSubject.Singleton;

            // Second request
            testSubject.CreateTagger(ValidBuffer);
            testSubject.Singleton.Should().Be(originalSingleton);

            CheckCreateFactoryCallCount(factoryMock, 1);
        }

        [TestMethod]
        public void SingletonTagger_DestroyedWhenNoActiveTaggers()
        {
            var testSubject = CreateTestSubject();
            testSubject.Singleton.Should().BeNull();

            var tagger1 = testSubject.CreateTagger(ValidBuffer);
            var tagger2 = testSubject.CreateTagger(ValidBuffer);

            testSubject.Singleton.Should().NotBeNull();

            ((IDisposable)tagger1).Dispose();
            testSubject.Singleton.Should().NotBeNull();

            ((IDisposable)tagger2).Dispose();
            testSubject.Singleton.Should().BeNull();
        }

        [TestMethod]
        public void SingletonTagger_DisposedIfDisposable()
        {
            var singletonMock = new Mock<ITagger<IDummyTagType>>();
            singletonMock.As<IDisposable>();
            TypedSingleton.CreateSingleton singletonFactory = _ => singletonMock.Object;

            // Create and dispose a tagger
            var testSubject = CreateTestSubject(singletonFactory);
            var tagger = testSubject.CreateTagger(ValidBuffer);
            ((IDisposable)tagger).Dispose();

            singletonMock.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void SingletonTagger_RecreatedWhenMoreTaggersRequested()
        {
            var factoryMock = CreateSingletonFactoryMock();

            var testSubject = CreateTestSubject(factoryMock.Object);

            // 1. Request a tagger
            var tagger1 = testSubject.CreateTagger(ValidBuffer);
            CheckCreateFactoryCallCount(factoryMock, 1);
            var originalSingleton = testSubject.Singleton;

            // 2. Dispose tagger -> singleton tagger is no longer required
            ((IDisposable)tagger1).Dispose();

            // 3. Request another tagger
            var _ = testSubject.CreateTagger(ValidBuffer);
            var currentSingleton = testSubject.Singleton;

            CheckCreateFactoryCallCount(factoryMock, 2);

            originalSingleton.Should().NotBeNull();
            currentSingleton.Should().NotBeNull();
            currentSingleton.Should().NotBe(originalSingleton);
        }

        [TestMethod]
        public void Flyweights_ActiveFlyweightsTrackedCorrectly()
        {
            var testSubject = CreateTestSubject();

            // Add multiple taggers
            var tagger1 = testSubject.CreateTagger(ValidBuffer);
            testSubject.ActiveTaggers.Count().Should().Be(1);

            var tagger2 = testSubject.CreateTagger(ValidBuffer);
            testSubject.ActiveTaggers.Count().Should().Be(2);

            var tagger3 = testSubject.CreateTagger(ValidBuffer);
            testSubject.ActiveTaggers.Count().Should().Be(3);

            // Dispose of taggers
            ((IDisposable)tagger1).Dispose();
            testSubject.ActiveTaggers.Should().BeEquivalentTo(tagger2, tagger3);

            ((IDisposable)tagger2).Dispose();
            testSubject.ActiveTaggers.Should().BeEquivalentTo(tagger3);

            ((IDisposable)tagger3).Dispose();
            testSubject.ActiveTaggers.Should().BeEmpty();
        }

        private static TypedSingleton CreateTestSubject(TypedSingleton.CreateSingleton singletonFactory = null)
        {
            singletonFactory = singletonFactory ?? CreateSingletonFactoryMock().Object;
            return new TypedSingleton(singletonFactory);
        }

        private static Mock<TypedSingleton.CreateSingleton> CreateSingletonFactoryMock()
        {
            var mock = new Mock<TypedSingleton.CreateSingleton>();
            mock.Setup(x => x.Invoke(It.IsAny<ITextBuffer>())).Returns(CreateNewSingleton);
            return mock;
        }

        private static ITagger<IDummyTagType> CreateNewSingleton() =>
            new Mock<ITagger<IDummyTagType>>().Object;

        private static void CheckCreateFactoryCallCount(Mock<TypedSingleton.CreateSingleton> factoryMock, int expected) =>
            factoryMock.Invocations.Count.Should().Be(expected);
    }
}
