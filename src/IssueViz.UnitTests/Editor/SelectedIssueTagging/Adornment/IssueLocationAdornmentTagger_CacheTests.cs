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
using Microsoft.VisualStudio.Text.Formatting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Adornment.IssueLocationAdornmentTagger;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.SelectedIssueTagging.Adornment
{
    [TestClass]
    public class IssueLocationAdornmentTagger_CacheTests
    {
        private static readonly ITextSnapshot ValidSnapshot = CreateSnapshot(length: 100);
        private static readonly IWpfTextView ValidTextView = CreateWpfTextView(ValidSnapshot);
        private static readonly IAnalysisIssueLocationVisualization ValidLocViz = CreateLocationViz(ValidSnapshot);

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void CreateOrUpdate_DoesNotExist_ReturnsNewAdornment()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);
            var newLocViz1 = CreateLocationViz();
            var newLocViz2 = CreateLocationViz();

            // Create an adornment
            var actual1 = testSubject.CreateOrUpdate(newLocViz1);

            actual1.LocationViz.Should().Be(newLocViz1);
            testSubject.CachedAdornments.Should().BeEquivalentTo(actual1);

            // Create another adornment
            var actual2 = testSubject.CreateOrUpdate(newLocViz2);

            actual2.LocationViz.Should().Be(newLocViz2);
            actual1.Should().NotBe(actual2);
            testSubject.CachedAdornments.Should().BeEquivalentTo(actual1, actual2);
        }

        [TestMethod]
        public void CreateOrUpdate_Exists_ReturnsExistingAdornment()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);
            var locViz1 = CreateLocationViz();
            var locViz2 = CreateLocationViz();

            // Populate the cache
            var adornment1 = testSubject.CreateOrUpdate(locViz1);
            var adornment2 = testSubject.CreateOrUpdate(locViz2);
            testSubject.CachedAdornments.Should().BeEquivalentTo(adornment1, adornment2);

            // Act
            testSubject.CreateOrUpdate(locViz1).Should().Be(adornment1);
            testSubject.CreateOrUpdate(locViz2).Should().Be(adornment2);

            testSubject.CachedAdornments.Should().BeEquivalentTo(adornment1, adornment2);
        }

        [TestMethod]
        public void CreateOrUpdate_Exists_AdornmentIsUpdated()
        {
            var lineSource1 = CreateFormattedLineSource(12d);
            var lineSource2 = CreateFormattedLineSource(18d);
            var textView = CreateWpfTextView(ValidSnapshot, lineSource1);
            var testSubject = new CachingAdornmentFactory(textView);

            var locViz = CreateLocationViz();

            var actual1 = testSubject.CreateOrUpdate(locViz);
            actual1.FontSize.Should().Be(12d); // sanity check

            ChangedMockedLineSource(textView, lineSource2);

            // Act
            var actual2 = testSubject.CreateOrUpdate(locViz);
            actual2.FontSize.Should().Be(18d);
        }

        [TestMethod]
        public void RemoveUnused_EmptyCache_NoError()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);

            Action act = () => testSubject.RemoveUnused(new[] { ValidLocViz });
            act.Should().NotThrow();
        }

        [TestMethod]
        public void RemoveUnused_NoCurrentLocations_AllRemoved()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);
            testSubject.CreateOrUpdate(CreateLocationViz());
            testSubject.CreateOrUpdate(CreateLocationViz());
            testSubject.CreateOrUpdate(CreateLocationViz());
            testSubject.CachedAdornments.Count.Should().Be(3); // sanity check

            testSubject.RemoveUnused(Array.Empty<IAnalysisIssueLocationVisualization>());
            testSubject.CachedAdornments.Should().BeEmpty();
        }

        [TestMethod]
        public void RemoveUnused_AllLocationsAreCurrent_NoAdormentsRemoved()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);

            var locViz1 = CreateLocationViz();
            var locViz2 = CreateLocationViz();

            var adornment1 = testSubject.CreateOrUpdate(locViz1);
            var adornment2 = testSubject.CreateOrUpdate(locViz2);
            var expectedAdornments = new[] { adornment1, adornment2 };

            // Act
            testSubject.RemoveUnused(new[] { locViz1, locViz2 });
            testSubject.CachedAdornments.Should().BeEquivalentTo(expectedAdornments);
        }

        [TestMethod]
        public void RemoveUnused_NotAllLocationsAreCurrent_ExpectedEntriesRemoved()
        {
            var testSubject = new CachingAdornmentFactory(ValidTextView);

            var locViz1 = CreateLocationViz();
            var locViz2 = CreateLocationViz();
            var locViz3 = CreateLocationViz();

            var adornment1 = testSubject.CreateOrUpdate(locViz1);
            var adornment2 = testSubject.CreateOrUpdate(locViz2);
            var adornment3 = testSubject.CreateOrUpdate(locViz3);
            testSubject.CachedAdornments.Should().BeEquivalentTo(new[] { adornment1, adornment2, adornment3 }); // sanity check

            // Remove multiple items
            testSubject.RemoveUnused(new[] { locViz3 });
            testSubject.CachedAdornments.Should().BeEquivalentTo(new[] { adornment3 });

            // Remove the remaining item
            var newLocViz = CreateLocationViz();
            testSubject.RemoveUnused(new[] { newLocViz });
            testSubject.CachedAdornments.Should().BeEmpty();
        }

        private static void ChangedMockedLineSource(IWpfTextView textView, IFormattedLineSource newLineSource)
        {
            var mock = ((IMocked<IWpfTextView>)textView).Mock;
            mock.Setup(x => x.FormattedLineSource).Returns(newLineSource);
        }
    }
}
