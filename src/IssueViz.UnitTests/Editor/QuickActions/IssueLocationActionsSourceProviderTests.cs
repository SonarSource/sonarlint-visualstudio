﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions
{
    [TestClass]
    public class IssueLocationActionsSourceProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueLocationActionsSourceProvider, ISuggestedActionsSourceProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(),
                MefTestHelpers.CreateExport<IIssueSelectionService>(),
                MefTestHelpers.CreateExport<ILightBulbBroker>());
        }

        [TestMethod]
        public void CreateSuggestedActionsSource_TextViewIsNull_Null()
        {
            var buffer = Mock.Of<ITextBuffer>();
            var testSubject = CreateTestSubject(buffer);

            var actionsSource = testSubject.CreateSuggestedActionsSource(null, buffer);
            actionsSource.Should().BeNull();
        }

        [TestMethod]
        public void CreateSuggestedActionsSource_TextBufferIsNull_Null()
        {
            ITextBuffer buffer = null;
            var testSubject = CreateTestSubject(buffer);

            var actionsSource = testSubject.CreateSuggestedActionsSource(Mock.Of<ITextView>(), buffer);
            actionsSource.Should().BeNull();
        }

        [TestMethod]
        public void CreateSuggestedActionsSource_TextViewIsNotNull_IssueLocationActionsSource()
        {
            var textView = CreateWpfTextView();
            var testSubject = CreateTestSubject(textView.TextBuffer);

            var actionsSource = testSubject.CreateSuggestedActionsSource(textView, textView.TextBuffer);
            actionsSource.Should().NotBeNull();
            actionsSource.Should().BeOfType<IssueLocationActionsSource>();
        }

        private static IssueLocationActionsSourceProvider CreateTestSubject(ITextBuffer buffer)
        {
            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<ISelectedIssueLocationTag>(buffer))
                .Returns(Mock.Of<ITagAggregator<ISelectedIssueLocationTag>>());

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(buffer))
                .Returns(Mock.Of<ITagAggregator<IIssueLocationTag>>());

            var selectionService = Mock.Of<IIssueSelectionService>();
            var serviceProvider = Mock.Of<IServiceProvider>();
            var lightBulbBroker = Mock.Of<ILightBulbBroker>();

            return new IssueLocationActionsSourceProvider(serviceProvider, bufferTagAggregatorFactoryService.Object, selectionService, lightBulbBroker);
        }
    }
}
