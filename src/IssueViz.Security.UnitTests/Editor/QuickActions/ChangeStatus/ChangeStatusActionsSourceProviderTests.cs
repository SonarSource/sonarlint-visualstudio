/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Security.Editor.QuickActions.ChangeStatus;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Editor.QuickActions.ChangeStatus;

[TestClass]
public class ChangeStatusActionsSourceProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ChangeStatusActionsSourceProvider, ISuggestedActionsSourceProvider>(
            MefTestHelpers.CreateExport<IBufferTagAggregatorFactoryService>(),
            MefTestHelpers.CreateExport<ILightBulbBroker>(),
            MefTestHelpers.CreateExport<IMuteIssuesService>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void CreateSuggestedActionsSource_TextViewIsNull_Null()
    {
        var buffer = Substitute.For<ITextBuffer>();
        var testSubject = CreateTestSubject(buffer);

        var actionsSource = testSubject.CreateSuggestedActionsSource(null, buffer);
        actionsSource.Should().BeNull();
    }

    [TestMethod]
    public void CreateSuggestedActionsSource_TextBufferIsNull_Null()
    {
        ITextBuffer buffer = null;
        var testSubject = CreateTestSubject(buffer);

        var actionsSource = testSubject.CreateSuggestedActionsSource(Substitute.For<ITextView>(), buffer);
        actionsSource.Should().BeNull();
    }

    [TestMethod]
    public void CreateSuggestedActionsSource_ProjectionBuffer_Null()
    {
        var textView = Substitute.For<ITextView>();
        IProjectionBuffer buffer = Substitute.For<IProjectionBuffer>();
        var testSubject = CreateTestSubject(buffer);

        var actionsSource = testSubject.CreateSuggestedActionsSource(textView, buffer);
        actionsSource.Should().BeNull();
    }

    [TestMethod]
    public void CreateSuggestedActionsSource_ValidArguments_ReturnsChangeStatusActionsSource()
    {
        var textView = Substitute.For<ITextView>();
        var buffer = Substitute.For<ITextBuffer>();
        var testSubject = CreateTestSubject(buffer);

        var actionsSource = testSubject.CreateSuggestedActionsSource(textView, buffer);
        actionsSource.Should().NotBeNull();
        actionsSource.Should().BeOfType<ChangeStatusActionsSource>();
    }

    private static ChangeStatusActionsSourceProvider CreateTestSubject(ITextBuffer buffer)
    {
        var bufferTagAggregatorFactoryService = Substitute.For<IBufferTagAggregatorFactoryService>();
        bufferTagAggregatorFactoryService
            .CreateTagAggregator<IIssueLocationTag>(buffer)
            .Returns(Substitute.For<ITagAggregator<IIssueLocationTag>>());

        var lightBulbBroker = Substitute.For<ILightBulbBroker>();
        var logger = Substitute.For<ILogger>();
        logger.ForVerboseContext(Arg.Any<string[]>()).Returns(logger);

        return new ChangeStatusActionsSourceProvider(
            bufferTagAggregatorFactoryService,
            lightBulbBroker,
            Substitute.For<IMuteIssuesService>(),
            logger,
            new NoOpThreadHandler());
    }
}
