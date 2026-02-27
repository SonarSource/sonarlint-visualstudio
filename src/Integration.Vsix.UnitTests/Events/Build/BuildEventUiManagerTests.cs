/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Events.Build;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;

namespace SonarLint.VisualStudio.Integration.UnitTests.Events.Build;

[TestClass]
public class BuildEventUiManagerTests
{
    private ISonarLintSettings settings = null!;
    private IErrorNotificationDialogService dialogService = null!;
    private IToolWindowService toolWindowService = null!;
    private ILocalIssuesStore localIssuesStore = null!;
    private NoOpThreadHandler threadHandling = null!;
    private BuildEventUiManager testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        settings = Substitute.For<ISonarLintSettings>();
        dialogService = Substitute.For<IErrorNotificationDialogService>();
        toolWindowService = Substitute.For<IToolWindowService>();
        localIssuesStore = Substitute.For<ILocalIssuesStore>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        settings.IsShowBuildErrorNotificationEnabled.Returns(true);
        localIssuesStore.GetAll().Returns([]);
        testSubject = new BuildEventUiManager(settings, dialogService, toolWindowService, localIssuesStore, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BuildEventUiManager, IBuildEventUiManager>(
            MefTestHelpers.CreateExport<ISonarLintSettings>(),
            MefTestHelpers.CreateExport<IErrorNotificationDialogService>(),
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<ILocalIssuesStore>(),
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<BuildEventUiManager>();

    [TestMethod]
    public void ShowErrorNotificationDialog_SettingDisabled_DoesNotCallDialogService()
    {
        settings.IsShowBuildErrorNotificationEnabled.Returns(false);

        testSubject.ShowErrorNotificationDialog();

        dialogService.DidNotReceiveWithAnyArgs().ShowDialog(default);
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_NoIssues_DoesNotCallDialogService()
    {
        localIssuesStore.GetAll().Returns([]);

        testSubject.ShowErrorNotificationDialog();

        dialogService.DidNotReceiveWithAnyArgs().ShowDialog(default);
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_OnlyNonErrorIssues_DoesNotCallDialogService()
    {
        var issues = new[]
        {
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_WARNING),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_MESSAGE)
        };
        localIssuesStore.GetAll().Returns(issues);

        testSubject.ShowErrorNotificationDialog();

        dialogService.DidNotReceiveWithAnyArgs().ShowDialog(default);
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_HasErrors_OkClicked_DoNotShowAgainFalse_ShowsToolWindow()
    {
        SetupErrorIssues(1);
        dialogService.ShowDialog(1).Returns((true, false));

        testSubject.ShowErrorNotificationDialog();

        toolWindowService.Received(1).Show(IssueListIds.ErrorListId);
        settings.IsShowBuildErrorNotificationEnabled = true;
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_HasErrors_OkClicked_DoNotShowAgainTrue_ShowsToolWindowAndDisablesSetting()
    {
        SetupErrorIssues(1);
        dialogService.ShowDialog(1).Returns((true, true));

        testSubject.ShowErrorNotificationDialog();

        toolWindowService.Received(1).Show(IssueListIds.ErrorListId);
        settings.IsShowBuildErrorNotificationEnabled = false;
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_HasErrors_CancelClicked_DoNotShowAgainFalse_DoesNotShowToolWindow()
    {
        SetupErrorIssues(1);
        dialogService.ShowDialog(1).Returns((false, false));

        testSubject.ShowErrorNotificationDialog();

        toolWindowService.DidNotReceiveWithAnyArgs().Show(default);
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_HasErrors_CancelClicked_DoNotShowAgainTrue_DoesNotShowToolWindowButDisablesSetting()
    {
        SetupErrorIssues(1);
        dialogService.ShowDialog(1).Returns((false, true));

        testSubject.ShowErrorNotificationDialog();

        toolWindowService.DidNotReceiveWithAnyArgs().Show(default);
        settings.IsShowBuildErrorNotificationEnabled = false;
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_MixedSeverities_CountsOnlyErrors()
    {
        var issues = new[]
        {
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_WARNING),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR),
            CreateIssueWithSeverity(__VSERRORCATEGORY.EC_MESSAGE)
        };
        localIssuesStore.GetAll().Returns(issues);
        dialogService.ShowDialog(2).Returns((false, false));

        testSubject.ShowErrorNotificationDialog();

        dialogService.Received(1).ShowDialog(2);
    }

    [TestMethod]
    public void ShowErrorNotificationDialog_HasErrors_RunsDialogOnUIThread()
    {
        SetupErrorIssues(1);
        dialogService.ShowDialog(1).Returns((false, false));

        testSubject.ShowErrorNotificationDialog();

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    private void SetupErrorIssues(int count)
    {
        var issues = Enumerable.Range(0, count)
            .Select(_ => CreateIssueWithSeverity(__VSERRORCATEGORY.EC_ERROR))
            .ToArray();
        localIssuesStore.GetAll().Returns(issues);
    }

    private static IAnalysisIssueVisualization CreateIssueWithSeverity(__VSERRORCATEGORY severity)
    {
        var issue = Substitute.For<IAnalysisIssueVisualization>();
        issue.VsSeverity.Returns(severity);
        return issue;
    }
}
