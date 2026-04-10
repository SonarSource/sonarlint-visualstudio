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

using SonarLint.VisualStudio.ConnectedMode.Transition;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Editor.QuickActions.ChangeStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Editor.QuickActions.ChangeStatus;

[TestClass]
public class ChangeStatusSuggestedActionTests
{
    private IAnalysisIssueVisualization issueVisualization = null!;
    private IMuteIssuesService muteIssuesService = null!;
    private ChangeStatusSuggestedAction testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        issueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        muteIssuesService = Substitute.For<IMuteIssuesService>();

        testSubject = new ChangeStatusSuggestedAction(issueVisualization, muteIssuesService);
    }

    [TestMethod]
    public void DisplayText_ReturnsExpectedText()
    {
        testSubject.DisplayText.Should().Be(Resources.ChangeStatusActionText);
    }

    [TestMethod]
    public void Invoke_CallsResolveIssueWithDialog()
    {
        const string serverKey = "some-server-key";
        issueVisualization.IssueServerKey.Returns(serverKey);

        testSubject.Invoke(CancellationToken.None);

        muteIssuesService.Received(1).ResolveIssueWithDialog("some-server-key", isTaintIssue: false);
    }

    [TestMethod]
    public void Invoke_CancellationTokenIsCancelled_NoChanges()
    {
        testSubject.Invoke(new CancellationToken(canceled: true));

        muteIssuesService.DidNotReceiveWithAnyArgs().ResolveIssueWithDialog(default, default);
    }

}
