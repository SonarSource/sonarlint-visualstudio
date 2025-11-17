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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenInIdeHandlerImplementationTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeHandlerImplementation, IOpenInIdeHandlerImplementation>(
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<ILocationNavigator>(),
            MefTestHelpers.CreateExport<IOpenInIdeNotification>(),
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<IIssueSelectionService>(),
            MefTestHelpers.CreateExport<IOpenInIdeConfigScopeValidator>(),
            MefTestHelpers.CreateExport<IOpenInIdeConverterImplementation>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenIssueInIdeHandler>();
    }

    [TestMethod]
    public void ShowIssue_BringsIdeToFrontAndRunsOnBackgroundThread()
    {
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling, out _, out _, out var notification, out _,
            out var windowService, out _, out _);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, default);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        notification.Received().Clear();
        windowService.Received().BringToFront();
    }

    [TestMethod]
    public void ShowIssue_IncorrectConfigScope_ShowsMessageBox()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string failureReason = "incorrect config scope";
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out _, out _, out var configScopeValidator, out var converter,
            out var notification, out var toolWindowService, out _, out _, out var issueSelectionService);
        configScopeValidator.TryGetConfigurationScopeRoot(configurationScope, out Arg.Any<string>(), out Arg.Any<string>())
            .Returns(info =>
            {
                info[2] = failureReason;
                return false;
            });

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        configScopeValidator.Received().TryGetConfigurationScopeRoot(configurationScope, out _, out _);
        converter.DidNotReceiveWithAnyArgs().TryConvert<IOpenInIdeIssue>(default, default, default, out _);
        notification.Received().InvalidRequest(failureReason, toolWindowId);
        toolWindowService.DidNotReceiveWithAnyArgs().Show(default);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }

    [TestMethod]
    public void ShowIssue_UnableToConvertIssue_ShowsMessageBox()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out _, out _, out var configScopeValidator, out var converter,
            out var notification, out var toolWindowService, out _, out _, out var issueSelectionService);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        converter.TryConvert(issue, configurationScopeRoot, dtoConverter, out Arg.Any<IAnalysisIssueVisualization>())
            .Returns(false);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        converter.Received().TryConvert(issue, configurationScopeRoot, dtoConverter, out Arg.Any<IAnalysisIssueVisualization>());
        notification.Received().InvalidRequest(OpenInIdeResources.ValidationReason_UnableToConvertIssue, toolWindowId);
        toolWindowService.DidNotReceiveWithAnyArgs().Show(default);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }

    [TestMethod]
    public void ShowIssue_NoProcessor_UsesConvertedIssue()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string issueFilePath = "file/path";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.CurrentFilePath.Returns(issueFilePath);
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out _, out _, out var configScopeValidator, out var converter,
            out var notification, out _, out _, out var navigator, out var issueSelectionService);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, dtoConverter, analysisIssueVisualization);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        navigator.Received().TryNavigatePartial(analysisIssueVisualization);
        notification.Received().UnableToOpenFile(issueFilePath, toolWindowId);
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
    }

    [TestMethod]
    public void ShowIssue_UnableToOpenFile_ShowsMessageBox()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string issueFilePath = "file/path";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.CurrentFilePath.Returns(issueFilePath);
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out var logger, out _, out var configScopeValidator, out var converter,
            out var notification, out _, out _, out var navigator, out var issueSelectionService);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, dtoConverter, analysisIssueVisualization);
        navigator.TryNavigatePartial(analysisIssueVisualization).Returns(NavigationResult.Failed);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        navigator.Received().TryNavigatePartial(analysisIssueVisualization);
        notification.Received().UnableToOpenFile(issueFilePath, toolWindowId);
        logger.AssertPartialOutputStringExists("[Open in IDE] Could not find the location at");
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
    }

    [TestMethod]
    public void ShowIssue_UnableToLocateIssue_ShowsMessageBox()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string issueFilePath = "file/path";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.CurrentFilePath.Returns(issueFilePath);
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out var logger, out _, out var configScopeValidator, out var converter,
            out var notification, out var toolWindowService, out _, out var navigator, out var issueSelectionService);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, dtoConverter, analysisIssueVisualization);
        navigator.TryNavigatePartial(analysisIssueVisualization).Returns(NavigationResult.OpenedFile);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        navigator.Received().TryNavigatePartial(analysisIssueVisualization);
        notification.Received().UnableToLocateIssue(issueFilePath, toolWindowId);
        logger.AssertPartialOutputStringExists("[Open in IDE] Could not find the location at");
        toolWindowService.Received().Show(toolWindowId);
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
    }

    [TestMethod]
    public void ShowIssue_ValidIssue_NavigatesToIt()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string ruleKey = "myrule:123";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var dtoConverter = Substitute.For<IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue>>();
        var testSubject = CreateTestSubject(out _, out _, out var configScopeValidator, out var converter,
            out var notification, out var toolWindowService, out _, out var navigator, out var issueSelectionService);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, dtoConverter, analysisIssueVisualization);
        SetUpNavigableIssue(navigator, analysisIssueVisualization);
        SetUpIssueRuleInfo(analysisIssueVisualization, ruleKey);

        testSubject.ShowIssue(issue, configurationScope, dtoConverter, toolWindowId);

        navigator.Received().TryNavigatePartial(analysisIssueVisualization);
        notification.Received().Clear();
        notification.DidNotReceiveWithAnyArgs().InvalidRequest(default, default);
        notification.DidNotReceiveWithAnyArgs().UnableToOpenFile(default, default);
        notification.DidNotReceiveWithAnyArgs().UnableToLocateIssue(default, default);
        toolWindowService.Received().Show(toolWindowId);
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
    }

    private static void SetUpNavigableIssue(
        ILocationNavigator navigator,
        IAnalysisIssueVisualization analysisIssueVisualization)
    {
        navigator.TryNavigatePartial(analysisIssueVisualization).Returns(NavigationResult.OpenedLocation);
    }

    private static void SetUpIssueRuleInfo(IAnalysisIssueVisualization analysisIssueVisualization, string ruleKey)
    {
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.RuleKey.Returns(ruleKey);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);
    }

    private static void SetUpIssueConversion(
        IOpenInIdeConverterImplementation converter,
        IOpenInIdeIssue issue,
        string configurationScopeRoot,
        IOpenInIdeIssueToAnalysisIssueConverter<IOpenInIdeIssue> dtoConverter,
        IAnalysisIssueVisualization analysisIssueVisualization)
    {
        converter.TryConvert(issue, configurationScopeRoot, dtoConverter, out Arg.Any<IAnalysisIssueVisualization>())
            .Returns(x =>
            {
                x[3] = analysisIssueVisualization;
                return true;
            });
    }

    private static void SetUpValidConfigScope(
        IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        string configurationScope,
        string configurationScopeRoot)
    {
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(configurationScope, out Arg.Any<string>(), out Arg.Any<string>()).Returns(
            x =>
            {
                x[1] = configurationScopeRoot;
                return true;
            });
    }

    private static void VerifyRunOnBackgroundThread(IThreadHandling threadHandling)
    {
        threadHandling.ReceivedWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
    }

    private static void VerifyProcessingLogWritten(TestLogger logger, string configurationScope, IOpenInIdeIssue issue)
    {
        logger.AssertOutputStringExists(string.Format(
            "[Open in IDE] Processing request. Configuration scope: {0}, Key: {1}, Type: {2}",
            configurationScope, issue.Key, issue.Type));
    }

    private OpenInIdeHandlerImplementation CreateTestSubject(
        out TestLogger logger,
        out IThreadHandling thereHandling,
        out IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        out IOpenInIdeConverterImplementation openInIdeConverterImplementation,
        out IOpenInIdeNotification notification,
        out IToolWindowService toolWindowService,
        out IIDEWindowService ideWindowService,
        out ILocationNavigator navigator,
        out IIssueSelectionService issueSelectionService)
    {
        logger = new TestLogger();
        thereHandling = Substitute.For<IThreadHandling>();
        SetUpThreadHandling(thereHandling);
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();
        openInIdeConverterImplementation = Substitute.For<IOpenInIdeConverterImplementation>();
        toolWindowService = Substitute.For<IToolWindowService>();
        notification = Substitute.For<IOpenInIdeNotification>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        navigator = Substitute.For<ILocationNavigator>();
        issueSelectionService = Substitute.For<IIssueSelectionService>();

        return new(openInIdeConfigScopeValidator, openInIdeConverterImplementation, toolWindowService, notification, ideWindowService, navigator,
            issueSelectionService, logger, thereHandling);
    }

    private IOpenInIdeIssue CreateDummyIssue()
    {
        var issueDetail = Substitute.For<IOpenInIdeIssue>();
        issueDetail.Key.Returns("key123");
        issueDetail.Type.Returns("type123");
        return issueDetail;
    }

    private void SetUpThreadHandling(IThreadHandling threadHandling)
    {
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()());
    }
}
