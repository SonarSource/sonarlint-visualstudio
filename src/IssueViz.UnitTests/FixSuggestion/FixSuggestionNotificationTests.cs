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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.FixSuggestion;

[TestClass]
public class FixSuggestionNotificationTests
{
    private FixSuggestionNotification testSubject;
    private IInfoBarManager infoBarManager;
    private IOutputWindowService outputWindowService;
    private IBrowserService browserService;
    private IThreadHandling threadHandler;

    [TestInitialize]
    public void TestInitialize()
    {
        infoBarManager = Substitute.For<IInfoBarManager>();
        outputWindowService = Substitute.For<IOutputWindowService>();
        threadHandler = new NoOpThreadHandler();
        browserService = Substitute.For<IBrowserService>();

        testSubject = new FixSuggestionNotification(infoBarManager, outputWindowService, browserService, threadHandler);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<FixSuggestionNotification, IFixSuggestionNotification>(
            MefTestHelpers.CreateExport<IInfoBarManager>(),
            MefTestHelpers.CreateExport<IOutputWindowService>(),
            MefTestHelpers.CreateExport<IBrowserService>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public async Task Clear_NoPreviousInfoBar_NoException()
    {
        await testSubject.ClearAsync();

        infoBarManager.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task Clear_HasPreviousInfoBar_InfoBarCleared()
    {
        var infoBar = MockInfoBar();
        await MockPreviousInfoBar(infoBar);
        
        await testSubject.ClearAsync();

        CheckInfoBarWithEventsRemoved(infoBar);
    }

    [TestMethod]
    public async Task Show_NoPreviousInfoBar_InfoBarIsShown()
    {
        var infoBarText = "info bar text";
        var infoBar = MockInfoBar();
        MockAttachInfoBarToMainWindow(infoBar);

        await testSubject.ShowAsync(infoBarText);

        CheckInfoBarWithEventsAdded(infoBar, infoBarText);
    }

    [TestMethod]
    public async Task Show_NoCustomText_InfoBarWithDefaultTextIsShown()
    {
        var infoBar = MockInfoBar(); 
        MockAttachInfoBarToMainWindow(infoBar);

        await testSubject.ShowAsync(null);

        CheckInfoBarWithEventsAdded(infoBar, FixSuggestionResources.InfoBarDefaultMessage);
    }

    [TestMethod]
    public async Task Show_HasPreviousInfoBar_InfoBarReplaced()
    {
        var firstInfoBar = MockInfoBar();
        var secondInfoBar = MockInfoBar();
        infoBarManager
            .AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>())
            .Returns(x => firstInfoBar, 
                x=> secondInfoBar);

        var text1 = "text1";
        await testSubject.ShowAsync(text1); // show first bar
        
        CheckInfoBarWithEventsAdded(firstInfoBar, text1);
        infoBarManager.ClearReceivedCalls();

        var text2 = "text2";
        await testSubject.ShowAsync(text2); // show second bar

        CheckInfoBarWithEventsRemoved(firstInfoBar);
        CheckInfoBarWithEventsAdded(secondInfoBar, text2);
    }

    [TestMethod]
    public async Task Dispose_HasPreviousInfoBar_InfoBarRemoved()
    {
        var infoBar = MockInfoBar();
        await MockPreviousInfoBar(infoBar);

        testSubject.Dispose();

        CheckInfoBarWithEventsRemoved(infoBar);
    }

    [TestMethod]
    public void Dispose_NoPreviousInfoBar_NoException()
    {
        Action act = () => testSubject.Dispose();

        act.Should().NotThrow();
    }

    [TestMethod]
    public async Task InfoBarIsManuallyClosed_InfoBarDetachedFromToolWindow()
    {
        var infoBar = MockInfoBar();
        await MockPreviousInfoBar(infoBar);

        infoBar.Closed += Raise.EventWith(EventArgs.Empty);

        CheckInfoBarWithEventsRemoved(infoBar);
    }

    [TestMethod]
    public async Task InfoBarShowLogsButtonClicked_OutputWindowIsShown()
    {
        var infoBar = MockInfoBar();
        await MockPreviousInfoBar(infoBar);

        infoBar.ButtonClick += Raise.EventWith(new InfoBarButtonClickedEventArgs(FixSuggestionResources.InfoBarButtonShowLogs));

        CheckInfoBarNotRemoved(infoBar);
        outputWindowService.Received(1).Show();
    }

    [TestMethod]
    public async Task InfoBarMoreInfoButtonClicked_DocumentationOpenInBrowser()
    {
        var infoBar = MockInfoBar();
        await MockPreviousInfoBar(infoBar);

        infoBar.ButtonClick += Raise.EventWith(new InfoBarButtonClickedEventArgs(FixSuggestionResources.InfoBarButtonMoreInfo));

        CheckInfoBarNotRemoved(infoBar);
        browserService.Received(1).Navigate(DocumentationLinks.OpenInIdeIssueLocation);
    }

    [TestMethod]
    public async Task ShowAsync_VerifySwitchesToUiThreadIsCalled()
    {
        MockAttachInfoBarToMainWindow(Substitute.For<IInfoBar>());
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling
            .When(x => x.RunOnUIThreadAsync(Arg.Any<Action>()))
            .Do(callInfo =>
            {
                infoBarManager.DidNotReceive().AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>());
                callInfo.Arg<Action>().Invoke();
                infoBarManager.Received(1).AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>());
            });
        var fixSuggestionNotification = new FixSuggestionNotification(infoBarManager,
            outputWindowService,
            browserService,
            threadHandling);

        await fixSuggestionNotification.ShowAsync( "some text");
        await threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public async Task ClearAsync_VerifySwitchesToUiThreadIsCalled()
    {
        MockAttachInfoBarToMainWindow(Substitute.For<IInfoBar>());
        var threadHandling = Substitute.For<IThreadHandling>();
        var fixSuggestionNotification = new FixSuggestionNotification(infoBarManager,
            outputWindowService,
            browserService,
            threadHandling);

        await fixSuggestionNotification.ClearAsync();

        await threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
    }

    [TestMethod]
    public async Task UnableToOpenFileAsync_CallsShowAsyncWithCorrectMessage()
    {
        var infoBar = MockInfoBar();
        MockAttachInfoBarToMainWindow(infoBar);
        var myPath = "c://myFile.cs";
        
        await testSubject.UnableToOpenFileAsync(myPath);

        CheckInfoBarWithEventsAdded(infoBar, string.Format(FixSuggestionResources.InfoBarUnableToOpenFile, myPath));
    }

    [TestMethod]
    public async Task InvalidRequestAsync_CallsShowAsyncWithCorrectMessage()
    {
        var infoBar = MockInfoBar();
        MockAttachInfoBarToMainWindow(infoBar);
        var reason = "wrong config scope";

        await testSubject.InvalidRequestAsync(reason);

        CheckInfoBarWithEventsAdded(infoBar, string.Format(FixSuggestionResources.InfoBarInvalidRequest, reason));
    }

    [TestMethod]
    public async Task UnableToLocateIssueAsync_CallsShowAsyncWithCorrectMessage()
    {
        var infoBar = MockInfoBar();
        MockAttachInfoBarToMainWindow(infoBar);
        var myPath = "c://myFile.cs";

        await testSubject.UnableToLocateIssueAsync(myPath);

        CheckInfoBarWithEventsAdded(infoBar, string.Format(FixSuggestionResources.InfoBarUnableToLocateFixSuggestion, myPath));
    }

    private async Task MockPreviousInfoBar(IInfoBar infoBar = null)
    {
        infoBar ??= MockInfoBar();
        MockAttachInfoBarToMainWindow(infoBar);
        var someText = "some text";

        await testSubject.ShowAsync(someText);

        CheckInfoBarWithEventsAdded(infoBar, someText); 
        outputWindowService.ReceivedCalls().Should().BeEmpty();
    }

    private void MockAttachInfoBarToMainWindow(IInfoBar infoBar)
    {
        infoBarManager
            .AttachInfoBarToMainWindow(Arg.Any<string>(), SonarLintImageMoniker.OfficialSonarLintMoniker, Arg.Any<string[]>())
            .Returns(infoBar);
    }

    private static IInfoBar MockInfoBar()
    {
        return Substitute.For<IInfoBar>();
    }

    private void CheckInfoBarWithEventsRemoved(IInfoBar infoBar)
    {
        infoBarManager.Received(1).CloseInfoBar(infoBar);

        infoBar.Received(1).Closed -= Arg.Any<EventHandler>();
        infoBar.Received(1).ButtonClick -= Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }

    private void CheckInfoBarNotRemoved(IInfoBar infoBar)
    {
        infoBarManager.DidNotReceive().CloseInfoBar(infoBar);
    }

    private void CheckInfoBarWithEventsAdded(IInfoBar infoBar, string text)
    {
        text ??= FixSuggestionResources.InfoBarDefaultMessage;
        var buttonTexts = new[]{FixSuggestionResources.InfoBarButtonMoreInfo, FixSuggestionResources.InfoBarButtonShowLogs};
        infoBarManager.Received(1).AttachInfoBarToMainWindow(
                text,
                SonarLintImageMoniker.OfficialSonarLintMoniker,
                buttonTexts);

        infoBar.Received(1).Closed += Arg.Any<EventHandler>();
        infoBar.Received(1).ButtonClick += Arg.Any<EventHandler<InfoBarButtonClickedEventArgs>>();
    }
}
