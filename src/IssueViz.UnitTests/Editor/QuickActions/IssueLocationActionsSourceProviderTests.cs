/*
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions;
using SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.TestInfrastructure;
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
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(),
                MefTestHelpers.CreateExport<IIssueSelectionService>(),
                MefTestHelpers.CreateExport<ILightBulbBroker>());
        }

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();
            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();
            var issueSelectionService = new Mock<IIssueSelectionService>();
            var lightBulbBroker = new Mock<ILightBulbBroker>();

            _ = new IssueLocationActionsSourceProvider(serviceOp.Object, bufferTagAggregatorFactoryService.Object,
                issueSelectionService.Object, lightBulbBroker.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceOp.Invocations.Should().BeEmpty();
            bufferTagAggregatorFactoryService.Invocations.Should().BeEmpty();
            issueSelectionService.Invocations.Should().BeEmpty();
            lightBulbBroker.Invocations.Should().BeEmpty();
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

        private static IVsUIServiceOperation CreateServiceOperation(IVsUIShell svcToPassToCallback)
        {
            var serviceOp = new Mock<IVsUIServiceOperation>();


            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<SVsUIShell, IVsUIShell, ISuggestedActionsSource>(It.IsAny<Func<IVsUIShell, ISuggestedActionsSource>>()))
               .Returns<Func<IVsUIShell, ISuggestedActionsSource>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
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
            var serviceOp = CreateServiceOperation(Mock.Of<IVsUIShell>());
            var lightBulbBroker = Mock.Of<ILightBulbBroker>();

            return new IssueLocationActionsSourceProvider(serviceOp, bufferTagAggregatorFactoryService.Object, selectionService, lightBulbBroker);
        }
    }
}
