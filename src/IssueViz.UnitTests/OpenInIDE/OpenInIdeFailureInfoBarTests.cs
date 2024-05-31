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

using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE
{
    [TestClass]
    public class OpenInIdeFailureInfoBarTests
    {
        private static readonly Guid ValidToolWindowId = Guid.NewGuid();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<OpenInIdeFailureInfoBar, IOpenInIdeFailureInfoBar>(
                MefTestHelpers.CreateExport<IInfoBarManager>(),
                MefTestHelpers.CreateExport<IOutputWindowService>(),
                MefTestHelpers.CreateExport<IBrowserService>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public async Task Clear_NoPreviousInfoBar_NoException()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ClearAsync();

            infoBarManager.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Clear_HasPreviousInfoBar_InfoBarCleared()
        {
            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBarManager, infoBar);

            SetupInfoBarEvents(infoBar);

            // Act
            await testSubject.ClearAsync();

            CheckInfoBarWithEventsRemoved(infoBarManager, infoBar);
        }

        [TestMethod]
        public async Task Show_NoPreviousInfoBar_InfoBarIsShown()
        {
            var infoBarText = "info bar text";
            var infoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId, infoBarText, default);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId, infoBarText, default);
            infoBar.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Show_NoMoreInfo_InfoBarHasOnlyShowLogsButton()
        {
            const bool hasMoreInfo = false;
            var infoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId, default, hasMoreInfo);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId, default, [OpenInIdeResources.InfoBar_Button_ShowLogs]);
            infoBar.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Show_HasMoreInfo_InfoBarHasMoreInfoAndShowLogsButtons()
        {
            const bool hasMoreInfo = true;
            var infoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId, default, hasMoreInfo);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId, default, [OpenInIdeResources.InfoBar_Button_MoreInfo, OpenInIdeResources.InfoBar_Button_ShowLogs]);
            infoBar.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Show_NoCustomText_InfoBarWithDefaultTextIsShown()
        {
            var infoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId, default, default);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId, OpenInIdeResources.DefaultInfoBarMessage, default);
            infoBar.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Show_HasPreviousInfoBar_InfoBarReplaced()
        {
            var firstInfoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(firstInfoBar);

            var secondInfoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(secondInfoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .SetupSequence(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(firstInfoBar.Object)
                .Returns(secondInfoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            // Act
            var text1 = "text1";
            await testSubject.ShowAsync(ValidToolWindowId, text1, default); // show first bar

            CheckInfoBarWithEventsAdded(infoBarManager, firstInfoBar, ValidToolWindowId, text1, default);
            infoBarManager.Invocations.Clear();

            var text2 = "text2";
            await testSubject.ShowAsync(ValidToolWindowId, text2, default); // show second bar

            CheckInfoBarWithEventsRemoved(infoBarManager, firstInfoBar);
            CheckInfoBarWithEventsAdded(infoBarManager, secondInfoBar, ValidToolWindowId, text2, default);

            firstInfoBar.VerifyNoOtherCalls();
            secondInfoBar.VerifyNoOtherCalls();
            infoBarManager.VerifyAll();
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Dispose_HasPreviousInfoBar_InfoBarRemoved()
        {
            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBarManager, infoBar);

            SetupInfoBarEvents(infoBar);

            // Act
            testSubject.Dispose();

            CheckInfoBarWithEventsRemoved(infoBarManager, infoBar);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_NoPreviousInfoBar_NoException()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), new NoOpThreadHandler());

            testSubject.Dispose();

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task InfoBarIsManuallyClosed_InfoBarDetachedFromToolWindow()
        {
            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBarManager, infoBar);

            SetupInfoBarEvents(infoBar);

            // Act
            infoBar.Raise(x => x.Closed += null, EventArgs.Empty);

            CheckInfoBarWithEventsRemoved(infoBarManager, infoBar);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task InfoBarShowLogsButtonClicked_OutputWindowIsShown()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var infoBar = new Mock<IInfoBar>();
            var outputWindowService = new Mock<IOutputWindowService>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBarManager, infoBar, outputWindow: outputWindowService);

            outputWindowService.VerifyNoOtherCalls();

            // Act
            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs(OpenInIdeResources.InfoBar_Button_ShowLogs));

            CheckInfoBarNotRemoved(infoBarManager, infoBar);
            outputWindowService.Verify(x => x.Show(), Times.Once);
            outputWindowService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task InfoBarMoreInfoButtonClicked_DocumentationOpenInBrowser()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var infoBar = new Mock<IInfoBar>();
            var browserService = new Mock<IBrowserService>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBarManager, infoBar, browserService: browserService);

            browserService.VerifyNoOtherCalls();

            // Act
            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs(OpenInIdeResources.InfoBar_Button_MoreInfo));

            CheckInfoBarNotRemoved(infoBarManager, infoBar);
            browserService.Verify(x => x.Navigate(DocumentationLinks.OpenInIdeIssueLocation));
            browserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ShowAsync_VerifySwitchesToUiThreadIsCalled()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager.Setup(x =>
                     x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default)).Returns(Mock.Of<IInfoBar>());

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>())).Callback<Action>(op =>
            {
                infoBarManager.Verify(x
                    => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default), Times.Never);
                op();
                infoBarManager.Verify(x
                    => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default), Times.Once);
            });

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), threadHandling.Object);

            await testSubject.ShowAsync(ValidToolWindowId, "some text", default);
            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);
        }

        [TestMethod]
        public async Task ClearAsync_VerifySwitchesToUiThreadIsCalled()
        {
            var threadHandling = new Mock<IThreadHandling>();

            var testSubject = new OpenInIdeFailureInfoBar(Mock.Of<IInfoBarManager>(), Mock.Of<IOutputWindowService>(), Mock.Of<IBrowserService>(), threadHandling.Object);

            await testSubject.ClearAsync();

            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);
        }

        private async static Task<OpenInIdeFailureInfoBar> CreateTestSubjectWithPreviousInfoBar(
            Mock<IInfoBarManager> infoBarManager = null,
            Mock<IInfoBar> infoBar = null, 
            Mock<IBrowserService> browserService = null,
            Mock<IOutputWindowService> outputWindow = null)
        {
            infoBar ??= new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            infoBarManager ??= new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButtons(ValidToolWindowId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), default))
                .Returns(infoBar.Object);

            browserService ??= new Mock<IBrowserService>();
            outputWindow ??= new Mock<IOutputWindowService>();

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, outputWindow.Object, browserService.Object, new NoOpThreadHandler());

            // Call "Show" to create an infobar and check it was added
            var someText = "some text";
            await testSubject.ShowAsync(ValidToolWindowId, someText, default);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId, someText, default);

            infoBarManager.VerifyNoOtherCalls();
            outputWindow.Invocations.Should().BeEmpty();

            return testSubject;
        }

        private static void SetupInfoBarEvents(Mock<IInfoBar> infoBar)
        {
            infoBar.SetupAdd(x => x.Closed += (sender, args) => { });
            infoBar.SetupAdd(x => x.ButtonClick += (sender, args) => { });

            infoBar.SetupRemove(x => x.Closed -= (sender, args) => { });
            infoBar.SetupRemove(x => x.ButtonClick -= (sender, args) => { });
        }

        private static void CheckInfoBarWithEventsRemoved(Mock<IInfoBarManager> infoBarManager, Mock<IInfoBar> infoBar)
        {
            infoBarManager.Verify(x => x.DetachInfoBar(infoBar.Object), Times.Once);

            infoBar.VerifyRemove(x => x.Closed -= It.IsAny<EventHandler>(), Times.Once);
            infoBar.VerifyRemove(x => x.ButtonClick -= It.IsAny<EventHandler<InfoBarButtonClickedEventArgs>>(), Times.Once);
        }

        private static void CheckInfoBarNotRemoved(Mock<IInfoBarManager> infoBarManager, Mock<IInfoBar> infoBar)
        {
            infoBarManager.Verify(x => x.DetachInfoBar(infoBar.Object), Times.Never);
        }

        private static void CheckInfoBarWithEventsAdded(Mock<IInfoBarManager> infoBarManager, Mock<IInfoBar> infoBar, Guid toolWindowId, string text, List<string> buttonTexts)
        {
            text ??= OpenInIdeResources.DefaultInfoBarMessage;
            buttonTexts ??= [OpenInIdeResources.InfoBar_Button_ShowLogs];
            infoBarManager.Verify(x => x.AttachInfoBarWithButtons(toolWindowId,
                    text,
                    It.Is<List<string>>(actualButtons => actualButtons.SequenceEqual(buttonTexts)),
                    default),
                Times.Once);

            infoBar.VerifyAdd(x => x.Closed += It.IsAny<EventHandler>(), Times.Once);
            infoBar.VerifyAdd(x => x.ButtonClick += It.IsAny<EventHandler<InfoBarButtonClickedEventArgs>>(), Times.Once);
        }
    }
}
