/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Editor.QuickActions.QuickFixes;
using static SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common.TaggerTestHelper;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.QuickActions.QuickFixes
{
    [TestClass]
    public class QuickFixActionsSourceProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<QuickFixActionsSourceProvider, ISuggestedActionsSourceProvider>(
                MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(),
                MefTestHelpers.CreateExport<ILightBulbBroker>(),
                MefTestHelpers.CreateExport<IQuickFixesTelemetryManager>(),
                MefTestHelpers.CreateExport<ILogger>());
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
        public void CreateSuggestedActionsSource_TextViewIsNotNull_QuickFixActionsSource()
        {
            var textView = CreateWpfTextView();
            var testSubject = CreateTestSubject(textView.TextBuffer);

            var actionsSource = testSubject.CreateSuggestedActionsSource(textView, textView.TextBuffer);
            actionsSource.Should().NotBeNull();
            actionsSource.Should().BeOfType<QuickFixActionsSource>();
        }

        private static QuickFixActionsSourceProvider CreateTestSubject(ITextBuffer buffer)
        {
            var bufferTagAggregatorFactoryService = new Mock<IBufferTagAggregatorFactoryService>();

            bufferTagAggregatorFactoryService
                .Setup(x => x.CreateTagAggregator<IIssueLocationTag>(buffer))
                .Returns(Mock.Of<ITagAggregator<IIssueLocationTag>>());

            var lightBulbBroker = Mock.Of<ILightBulbBroker>();

            return new QuickFixActionsSourceProvider(
                bufferTagAggregatorFactoryService.Object, 
                lightBulbBroker,
                Mock.Of<IQuickFixesTelemetryManager>(),
                Mock.Of<ILogger>());
        }
    }
}
