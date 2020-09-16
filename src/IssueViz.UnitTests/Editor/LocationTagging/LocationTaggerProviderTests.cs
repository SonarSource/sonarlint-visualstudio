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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class LocationTaggerProviderTests : CommonTaggerProviderTestsBase
    {
        private readonly IIssueLocationStore ValidLocationStore = Mock.Of<IIssueLocationStore>();
        private readonly IIssueSpanCalculator ValidSpanCalculator = Mock.Of<IIssueSpanCalculator>();
        private readonly ILogger ValidLogger = Mock.Of<ILogger>();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var storeExport = MefTestHelpers.CreateExport<IIssueLocationStore>(ValidLocationStore);
            var calculatorExport = MefTestHelpers.CreateExport<IIssueSpanCalculator>(ValidSpanCalculator);
            var taggableBufferIndicatorExport = MefTestHelpers.CreateExport<ITaggableBufferIndicator>(Mock.Of<ITaggableBufferIndicator>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(ValidLogger);

            MefTestHelpers.CheckTypeCanBeImported<LocationTaggerProvider, ITaggerProvider>(null, new[]
            {
                storeExport, calculatorExport, taggableBufferIndicatorExport, loggerExport
            });
        }

        internal override ITaggerProvider CreateTestSubject(ITaggableBufferIndicator taggableBufferIndicator) =>
            new LocationTaggerProvider(ValidLocationStore, ValidSpanCalculator, taggableBufferIndicator, ValidLogger);

        [TestMethod]
        public void SingletonManagement_OneBuffer_CreatesOneSingletonManager()
        {
            var buffer = CreateBuffer();
            var testSubject = CreateTestSubject(CreateTaggableBufferIndicator());
            GetSingletonManager(buffer).Should().BeNull();

            // 1. Request first tagger for buffer
            testSubject.CreateTagger<ITag>(buffer);
            var manager1 = GetSingletonManager(buffer);

            // 2. Request second tagger - expecting same singleton manager
            testSubject.CreateTagger<ITag>(buffer);
            var manager2 = GetSingletonManager(buffer);

            manager1.Should().NotBeNull();
            manager2.Should().NotBeNull();
            manager1.Should().BeSameAs(manager2);
        }

        [TestMethod]
        public void CreateTagger_SingletonManagement_TwoBuffer_CreatesSingletonManagerPerBuffer()
        {
            var buffer1 = CreateBuffer();
            var buffer2 = CreateBuffer();
            var testSubject = CreateTestSubject(CreateTaggableBufferIndicator());

            // 1. Request tagger for first buffer
            testSubject.CreateTagger<ITag>(buffer1);
            var manager1 = GetSingletonManager(buffer1);

            // 2. Request tagger for second buffer - expecting a different instance
            testSubject.CreateTagger<ITag>(buffer2);
            var manager2 = GetSingletonManager(buffer2);

            manager1.Should().NotBeNull();
            manager2.Should().NotBeNull();
            manager1.Should().NotBeSameAs(manager2);
        }

        private static SingletonDisposableTaggerManager<IIssueLocationTag> GetSingletonManager(ITextBuffer buffer)
        {
            buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IIssueLocationTag>>(
                typeof(SingletonDisposableTaggerManager<IIssueLocationTag>), out var manager);
            return manager;
        }
    }
}
