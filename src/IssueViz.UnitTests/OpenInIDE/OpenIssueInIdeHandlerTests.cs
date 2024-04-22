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

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenIssueInIdeHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenIssueInIdeHandler, IOpenIssueInIdeHandler>(
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<ILocationNavigator>(),
            MefTestHelpers.CreateExport<IEducation>(),
            MefTestHelpers.CreateExport<IOpenInIdeFailureInfoBar>(),
            MefTestHelpers.CreateExport<IIssueSelectionService>(),
            MefTestHelpers.CreateExport<IOpenInIdeConfigScopeValidator>(),
            MefTestHelpers.CreateExport<IOpenInIdeConverter>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenIssueInIdeHandler>();
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ShowIssue_IncorrectConfigScope_ShowsInfoBar(bool isTaint)
    {
        var issue = CreateDummyIssue(isTaint);
        const string configurationScope = "scope";
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out _, out var infoBarManager,
            out var windowService, out _, out var issueSelectionService, out _);
        configScopeValidator.TryGetConfigurationScopeRoot(configurationScope, out Arg.Any<string>())
            .Returns(false);

        testSubject.ShowIssue(issue, configurationScope);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        configScopeValidator.Received().TryGetConfigurationScopeRoot(configurationScope, out _);
        VerifyCorrectInfoBarShown(isTaint, infoBarManager);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ShowIssue_UnableToConvertIssue_ShowsInfoBar(bool isTaint)
    {
        var issue = CreateDummyIssue(isTaint);
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var converter, out var infoBarManager,
            out var windowService, out _, out var issueSelectionService, out _);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        converter.TryConvertIssue(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>())
            .Returns(false);

        testSubject.ShowIssue(issue, configurationScope);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        converter.Received().TryConvertIssue(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>());
        VerifyCorrectInfoBarShown(isTaint, infoBarManager);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }
    
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ShowIssue_UnableToLocateIssue_ShowsInfoBar(bool isTaint)
    {
        var issue = CreateDummyIssue(isTaint);
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var converter, out var infoBarManager,
            out var windowService, out var navigator, out var issueSelectionService, out _);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(false);

        testSubject.ShowIssue(issue, configurationScope);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        logger.AssertPartialOutputStringExists("[Open in IDE] Could not find the issue at ");
        windowService.Received().BringToFront();
        navigator.Received().TryNavigate(analysisIssueVisualization);
        VerifyCorrectInfoBarShown(isTaint, infoBarManager);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }
    
    [TestMethod]
    public void ShowIssue_ValidIssue_NavigatesToIt()
    {
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string ruleKey = "myrule:123";
        const string ruleContext = "myrulecontext";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        SetUpIssueRuleInfo(analysisIssueVisualization, ruleKey, ruleContext);
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var converter, out var infoBarManager,
            out var windowService, out var navigator, out var issueSelectionService, out var education);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(true);

        testSubject.ShowIssue(issue, configurationScope);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        navigator.Received().TryNavigate(analysisIssueVisualization);
        infoBarManager.DidNotReceiveWithAnyArgs().ShowAsync(default);
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
        education.Received().ShowRuleHelp(Arg.Is<SonarCompositeRuleId>(id => id.ErrorListErrorCode == ruleKey), ruleContext);
    }
    
    [TestMethod]
    public void ShowIssue_ValidIssueWithUnsupportedRuleKey_NavigatesToItButIgnoresRuleDescription()
    {
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        const string ruleKey = "myruleISNOTSUPPORTED";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        SetUpIssueRuleInfo(analysisIssueVisualization, ruleKey, default);
        var testSubject = CreateTestSubject(out _, out _,
            out var configScopeValidator, out var converter, out var infoBarManager,
            out _, out var navigator, out var issueSelectionService, out var education);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(true);

        testSubject.ShowIssue(issue, configurationScope);
        
        infoBarManager.DidNotReceiveWithAnyArgs().ShowAsync(default);
        issueSelectionService.Received().SelectedIssue = analysisIssueVisualization;
        education.DidNotReceiveWithAnyArgs().ShowRuleHelp(default, default);
    }

    private static void SetUpIssueRuleInfo(IAnalysisIssueVisualization analysisIssueVisualization, string ruleKey, string ruleContext)
    {
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.RuleKey.Returns(ruleKey);
        analysisIssueBase.RuleDescriptionContextKey.Returns(ruleContext);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);
    }

    private static void SetUpIssueConversion(IOpenInIdeConverter converter, IssueDetailDto issue,
        string configurationScopeRoot, IAnalysisIssueVisualization analysisIssueVisualization)
    {
        converter.TryConvertIssue(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>())
            .Returns(x =>
            {
                x[2] = analysisIssueVisualization;
                return true;
            });
    }

    private static void SetUpValidConfigScope(IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        string configurationScope, string configurationScopeRoot)
    {
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(configurationScope, out Arg.Any<string>()).Returns(
            x =>
            {
                x[1] = configurationScopeRoot;
                return true;
            });
    }

    private static void VerifyCorrectInfoBarShown(bool isTaint, IOpenInIdeFailureInfoBar infoBarManager)
    {
        infoBarManager.Received().ShowAsync(isTaint ? IssueListIds.TaintId : IssueListIds.ErrorListId);
    }

    private static void VerifyRunOnBackgroundThread(IThreadHandling threadHandling)
    {
        threadHandling.ReceivedWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
    }

    private static void VerifyProcessingLogWritten(TestLogger logger, string configurationScope, IssueDetailDto issue)
    {
        logger.AssertOutputStringExists(string.Format(
            "[Open in IDE] Processing request. Configuration scope: {0}, issue: {1}", configurationScope,
            issue.issueKey));
    }

    private OpenIssueInIdeHandler CreateTestSubject(out TestLogger logger,
        out IThreadHandling thereHandling,
        out IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
        out IOpenInIdeConverter openInIdeConverter,
        out IOpenInIdeFailureInfoBar infoBarManager,
        out IIDEWindowService ideWindowService,
        out ILocationNavigator navigator,
        out IIssueSelectionService issueSelectionService,
        out IEducation education)
    {
        logger = new TestLogger();
        thereHandling = Substitute.For<IThreadHandling>();
        SetUpThreadHandling(thereHandling);
        openInIdeConfigScopeValidator = Substitute.For<IOpenInIdeConfigScopeValidator>();
        openInIdeConverter = Substitute.For<IOpenInIdeConverter>();
        infoBarManager = Substitute.For<IOpenInIdeFailureInfoBar>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        navigator = Substitute.For<ILocationNavigator>();
        issueSelectionService = Substitute.For<IIssueSelectionService>();
        education = Substitute.For<IEducation>();

        return new(openInIdeConfigScopeValidator, openInIdeConverter, infoBarManager, ideWindowService, navigator,
            issueSelectionService, education, logger, thereHandling);
    }

    private IssueDetailDto CreateDummyIssue(bool isTaint = false) => new("someissueid", default, default, default,
        default, default, default, default, isTaint, default, default);

    private void SetUpThreadHandling(IThreadHandling threadHandling)
    {
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()());
    }
}
