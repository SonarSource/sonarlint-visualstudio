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
public class OpenInIdeHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeHandler, IOpenInIdeHandler>(
            MefTestHelpers.CreateExport<IIDEWindowService>(),
            MefTestHelpers.CreateExport<ILocationNavigator>(),
            MefTestHelpers.CreateExport<IEducation>(),
            MefTestHelpers.CreateExport<IOpenInIdeFailureInfoBar>(),
            MefTestHelpers.CreateExport<IIssueSelectionService>(),
            MefTestHelpers.CreateExport<IOpenInIdeConfigScopeValidator>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenIssueInIdeHandler>();
    }

    [TestMethod]
    public void ShowIssue_IncorrectConfigScope_ShowsInfoBar()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        var converter = Substitute.For<IOpenInIdeConverter<IIssueDetail>>();
        var visualizationProcessor = Substitute.For<IOpenInIdeVisualizationProcessor>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var infoBarManager,
            out var windowService, out _, out var issueSelectionService, out _);
        configScopeValidator.TryGetConfigurationScopeRoot(configurationScope, out Arg.Any<string>())
            .Returns(false);

        testSubject.ShowIssue(issue, configurationScope, converter, toolWindowId, visualizationProcessor);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        configScopeValidator.Received().TryGetConfigurationScopeRoot(configurationScope, out _);
        VerifyCorrectInfoBarShown(toolWindowId, infoBarManager);
        visualizationProcessor.DidNotReceiveWithAnyArgs().HandleConvertedIssue(default);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }

    [TestMethod]
    public void ShowIssue_UnableToConvertIssue_ShowsInfoBar()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        var converter = Substitute.For<IOpenInIdeConverter<IIssueDetail>>();
        var visualizationProcessor = Substitute.For<IOpenInIdeVisualizationProcessor>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var infoBarManager,
            out var windowService, out _, out var issueSelectionService, out _);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        converter.TryConvert(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>())
            .Returns(false);

        testSubject.ShowIssue(issue, configurationScope, converter, toolWindowId,visualizationProcessor);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        converter.Received().TryConvert(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>());
        VerifyCorrectInfoBarShown(toolWindowId, infoBarManager);
        visualizationProcessor.DidNotReceiveWithAnyArgs().HandleConvertedIssue(default);
        issueSelectionService.DidNotReceiveWithAnyArgs().SelectedIssue = default;
    }
    
    [TestMethod]
    public void ShowIssue_UnableToLocateIssue_ShowsInfoBar()
    {
        var toolWindowId = Guid.NewGuid();
        var issue = CreateDummyIssue();
        const string configurationScope = "scope";
        const string configurationScopeRoot = "root/of/scope";
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var converter = Substitute.For<IOpenInIdeConverter<IIssueDetail>>();
        var visualizationProcessor = Substitute.For<IOpenInIdeVisualizationProcessor>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var infoBarManager,
            out var windowService, out var navigator, out var issueSelectionService, out _);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(false);

        testSubject.ShowIssue(issue, configurationScope, converter, toolWindowId, visualizationProcessor);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        logger.AssertPartialOutputStringExists("[Open in IDE] Could not find the location at ");
        windowService.Received().BringToFront();
        navigator.Received().TryNavigate(analysisIssueVisualization);
        VerifyCorrectInfoBarShown(toolWindowId, infoBarManager);
        visualizationProcessor.Received().HandleConvertedIssue(analysisIssueVisualization);
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
        var converter = Substitute.For<IOpenInIdeConverter<IIssueDetail>>();
        var visualizationProcessor = Substitute.For<IOpenInIdeVisualizationProcessor>();
        var testSubject = CreateTestSubject(out var logger, out var threadHandling,
            out var configScopeValidator, out var infoBarManager,
            out var windowService, out var navigator, out var issueSelectionService, out var education);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(true);

        testSubject.ShowIssue(issue, configurationScope, converter, Guid.NewGuid(), visualizationProcessor);

        VerifyRunOnBackgroundThread(threadHandling);
        VerifyProcessingLogWritten(logger, configurationScope, issue);
        windowService.Received().BringToFront();
        navigator.Received().TryNavigate(analysisIssueVisualization);
        infoBarManager.Received().ClearAsync();
        infoBarManager.DidNotReceiveWithAnyArgs().ShowAsync(default);
        visualizationProcessor.Received().HandleConvertedIssue(analysisIssueVisualization);
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
        var converter = Substitute.For<IOpenInIdeConverter<IIssueDetail>>();
        var testSubject = CreateTestSubject(out _, out _,
            out var configScopeValidator, out var infoBarManager,
            out _, out var navigator, out var issueSelectionService, out var education);
        SetUpValidConfigScope(configScopeValidator, configurationScope, configurationScopeRoot);
        SetUpIssueConversion(converter, issue, configurationScopeRoot, analysisIssueVisualization);
        navigator.TryNavigate(analysisIssueVisualization).Returns(true);

        testSubject.ShowIssue(issue, configurationScope, converter, Guid.NewGuid());

        infoBarManager.Received().ClearAsync();
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

    private static void SetUpIssueConversion(IOpenInIdeConverter<IIssueDetail> converter, IIssueDetail issue,
        string configurationScopeRoot, IAnalysisIssueVisualization analysisIssueVisualization)
    {
        converter.TryConvert(issue, configurationScopeRoot, out Arg.Any<IAnalysisIssueVisualization>())
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

    private static void VerifyCorrectInfoBarShown(Guid toolWindowId, IOpenInIdeFailureInfoBar infoBarManager)
    {
        infoBarManager.Received().ShowAsync(toolWindowId);
    }

    private static void VerifyRunOnBackgroundThread(IThreadHandling threadHandling)
    {
        threadHandling.ReceivedWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
    }

    private static void VerifyProcessingLogWritten(TestLogger logger, string configurationScope, IIssueDetail issue)
    {
        logger.AssertOutputStringExists(string.Format(
            "[Open in IDE] Processing request. Configuration scope: {0}, Key: {1}, Type: {2}", 
            configurationScope, issue.Key, issue.Type));
    }

    private OpenInIdeHandler CreateTestSubject(out TestLogger logger,
        out IThreadHandling thereHandling,
        out IOpenInIdeConfigScopeValidator openInIdeConfigScopeValidator,
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
        infoBarManager = Substitute.For<IOpenInIdeFailureInfoBar>();
        ideWindowService = Substitute.For<IIDEWindowService>();
        navigator = Substitute.For<ILocationNavigator>();
        issueSelectionService = Substitute.For<IIssueSelectionService>();
        education = Substitute.For<IEducation>();

        return new(openInIdeConfigScopeValidator, infoBarManager, ideWindowService, navigator,
            issueSelectionService, education, logger, thereHandling);
    }

    private IIssueDetail CreateDummyIssue()
    {
        var issueDetail = Substitute.For<IIssueDetail>();
        issueDetail.Key.Returns("key123");
        issueDetail.Type.Returns("type123");
        return issueDetail;
    }

    private void SetUpThreadHandling(IThreadHandling threadHandling)
    {
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()());
    }
}
