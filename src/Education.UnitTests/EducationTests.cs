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

using System.Windows.Documents;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Education.Rule;
using SonarLint.VisualStudio.Education.XamlGenerator;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Education.UnitTests;

[TestClass]
public class EducationTests
{
    private readonly SonarCompositeRuleId knownRule = new("repoKey", "ruleKey");
    private readonly SonarCompositeRuleId unknownRule = new("known", "xxx");

    private ILogger logger;
    private IRuleHelpToolWindow ruleDescriptionToolWindow;
    private IRuleHelpXamlBuilder ruleHelpXamlBuilder;
    private IRuleInfo ruleInfo;
    private IRuleMetaDataProvider ruleMetadataProvider;
    private IShowRuleInBrowser showRuleInBrowser;
    private Education testSubject;
    private IThreadHandling threadHandling;
    private IToolWindowService toolWindowService;

    [TestInitialize]
    public void TestInitialize()
    {
        toolWindowService = Substitute.For<IToolWindowService>();
        ruleMetadataProvider = Substitute.For<IRuleMetaDataProvider>();
        showRuleInBrowser = Substitute.For<IShowRuleInBrowser>();
        ruleHelpXamlBuilder = Substitute.For<IRuleHelpXamlBuilder>();
        ruleDescriptionToolWindow = Substitute.For<IRuleHelpToolWindow>();
        ruleInfo = Substitute.For<IRuleInfo>();
        logger = new TestLogger(true);
        threadHandling = new NoOpThreadHandler();
        SetupKnownRule();
        SetupUnknownRule();

        testSubject = new Education(toolWindowService, ruleMetadataProvider, showRuleInBrowser, logger, ruleHelpXamlBuilder, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<Education, IEducation>(
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<IRuleMetaDataProvider>(),
            MefTestHelpers.CreateExport<IShowRuleInBrowser>(),
            MefTestHelpers.CreateExport<IRuleHelpXamlBuilder>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void Ctor_IsFreeThreaded()
    {
        toolWindowService.ReceivedCalls().Should().HaveCount(0);
        ruleMetadataProvider.ReceivedCalls().Should().HaveCount(0);
        showRuleInBrowser.ReceivedCalls().Should().HaveCount(0);
        ruleHelpXamlBuilder.ReceivedCalls().Should().HaveCount(0);
    }

    [TestMethod]
    public void ShowRuleHelp_KnownRule_DocumentIsDisplayedInToolWindow()
    {
        var flowDocument = MockFlowDocument();
        toolWindowService.GetToolWindow<RuleHelpToolWindow, IRuleHelpToolWindow>().Returns(ruleDescriptionToolWindow);

        testSubject.ShowRuleHelp(knownRule, null);

        VerifyGetsRuleInfoForCorrectRuleId(knownRule);
        VerifyRuleIsDisplayedInIde(flowDocument);
        VerifyRuleNotShownInBrowser();
    }

    [TestMethod]
    public void ShowRuleHelp_FailedToDisplayRule_RuleIsShownInBrowser()
    {
        ruleHelpXamlBuilder.When(x => x.Create(ruleInfo)).Do(x => throw new Exception("some layout error"));

        testSubject.ShowRuleHelp(knownRule, null);

        VerifyGetsRuleInfoForCorrectRuleId(knownRule);
        VerifyRuleShownInBrowser(knownRule);
        VerifyAttemptsToBuildRuleButFails();
    }

    [TestMethod]
    public void ShowRuleHelp_UnknownRule_RuleIsShownInBrowser()
    {
        testSubject.ShowRuleHelp(unknownRule, null);

        VerifyGetsRuleInfoForCorrectRuleId(unknownRule);
        VerifyRuleShownInBrowser(unknownRule);
        VerifyNotAttemptsBuildRule();
    }

    [TestMethod]
    public void ShowRuleHelp_FilterableIssueProvided_CallsGetRuleInfoForIssue()
    {
        var issueId = Guid.NewGuid();

        testSubject.ShowRuleHelp(knownRule, issueId);

        ruleMetadataProvider.Received(1).GetRuleInfoAsync(knownRule, issueId);
    }

    private void VerifyGetsRuleInfoForCorrectRuleId(SonarCompositeRuleId ruleId) => ruleMetadataProvider.Received(1).GetRuleInfoAsync(ruleId, Arg.Any<Guid?>());

    private void VerifyRuleShownInBrowser(SonarCompositeRuleId ruleId) => showRuleInBrowser.Received(1).ShowRuleDescription(ruleId);

    private void VerifyRuleNotShownInBrowser() => showRuleInBrowser.ReceivedCalls().Should().HaveCount(0);

    private void VerifyToolWindowShown() => toolWindowService.Received(1).Show(RuleHelpToolWindow.ToolWindowId);

    private void VerifyAttemptsToBuildRuleButFails()
    {
        ruleHelpXamlBuilder.ReceivedCalls().Should().HaveCount(1);
        toolWindowService.ReceivedCalls().Should().HaveCount(1);
    }

    private void VerifyNotAttemptsBuildRule()
    {
        ruleHelpXamlBuilder.ReceivedCalls().Should().HaveCount(0);
        toolWindowService.ReceivedCalls().Should().HaveCount(0);
    }

    private void VerifyRuleIsDisplayedInIde(FlowDocument flowDocument)
    {
        ruleHelpXamlBuilder.Received(1).Create(ruleInfo);
        ruleDescriptionToolWindow.Received(1).UpdateContent(flowDocument);
        VerifyToolWindowShown();
    }

    private void SetupKnownRule() => ruleMetadataProvider.GetRuleInfoAsync(knownRule, Arg.Any<Guid?>()).Returns(ruleInfo);

    private void SetupUnknownRule() => ruleMetadataProvider.GetRuleInfoAsync(unknownRule, Arg.Any<Guid?>()).ReturnsNull();

    private FlowDocument MockFlowDocument()
    {
        var flowDocument = Substitute.For<FlowDocument>();
        ruleHelpXamlBuilder.Create(ruleInfo).Returns(flowDocument);
        return flowDocument;
    }
}
