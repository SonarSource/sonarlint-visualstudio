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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public async Task Clear_NoPreviousInfoBar_NoException()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), new NoOpThreadHandler());

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
            var infoBar = new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId);
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
                .SetupSequence(x => x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(firstInfoBar.Object)
                .Returns(secondInfoBar.Object);

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), new NoOpThreadHandler());

            // Act
            await testSubject.ShowAsync(ValidToolWindowId); // show first bar

            CheckInfoBarWithEventsAdded(infoBarManager, firstInfoBar, ValidToolWindowId);
            infoBarManager.Invocations.Clear();

            await testSubject.ShowAsync(ValidToolWindowId); // show second bar

            CheckInfoBarWithEventsRemoved(infoBarManager, firstInfoBar);
            CheckInfoBarWithEventsAdded(infoBarManager, secondInfoBar, ValidToolWindowId);

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
            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), new NoOpThreadHandler());

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
        public async Task InfoBarButtonClicked_OutputWindowIsShown()
        {
            var infoBar = new Mock<IInfoBar>();
            var outputWindowService = new Mock<IOutputWindowService>();
            var testSubject = await CreateTestSubjectWithPreviousInfoBar(infoBar: infoBar, outputWindow: outputWindowService);

            outputWindowService.VerifyNoOtherCalls();

            // Act
            infoBar.Raise(x => x.ButtonClick += null, new InfoBarButtonClickedEventArgs("some button"));

            outputWindowService.Verify(x => x.Show(), Times.Once);
            outputWindowService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task ShowAsync_VerifySwitchesToUiThreadIsCalled()
        {
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager.Setup(x =>
                     x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Mock.Of<IInfoBar>());

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>())).Callback<Action>(op =>
            {
                infoBarManager.Verify(x
                    => x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
                op();
                infoBarManager.Verify(x
                    => x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
            });

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, Mock.Of<IOutputWindowService>(), threadHandling.Object);

            await testSubject.ShowAsync(ValidToolWindowId);
            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);
        }

        [TestMethod]
        public async Task ClearAsync_VerifySwitchesToUiThreadIsCalled()
        {
            var threadHandling = new Mock<IThreadHandling>();

            var testSubject = new OpenInIdeFailureInfoBar(Mock.Of<IInfoBarManager>(), Mock.Of<IOutputWindowService>(), threadHandling.Object);

            await testSubject.ClearAsync();

            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);
        }

        private async static Task<OpenInIdeFailureInfoBar> CreateTestSubjectWithPreviousInfoBar(
            Mock<IInfoBarManager> infoBarManager = null,
            Mock<IInfoBar> infoBar = null,
            Mock<IOutputWindowService> outputWindow = null)
        {
            infoBar ??= new Mock<IInfoBar>();
            SetupInfoBarEvents(infoBar);

            infoBarManager ??= new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(ValidToolWindowId, It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(infoBar.Object);

            outputWindow ??= new Mock<IOutputWindowService>();

            var testSubject = new OpenInIdeFailureInfoBar(infoBarManager.Object, outputWindow.Object, new NoOpThreadHandler());

            // Call "Show" to create an infobar and check it was added
            await testSubject.ShowAsync(ValidToolWindowId);

            CheckInfoBarWithEventsAdded(infoBarManager, infoBar, ValidToolWindowId);

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

        private static void CheckInfoBarWithEventsAdded(Mock<IInfoBarManager> infoBarManager, Mock<IInfoBar> infoBar, Guid toolWindowId)
        {
            infoBarManager.Verify(x => x.AttachInfoBarWithButton(toolWindowId, It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);

            infoBar.VerifyAdd(x => x.Closed += It.IsAny<EventHandler>(), Times.Once);
            infoBar.VerifyAdd(x => x.ButtonClick += It.IsAny<EventHandler<InfoBarButtonClickedEventArgs>>(), Times.Once);
        }
    }
}
