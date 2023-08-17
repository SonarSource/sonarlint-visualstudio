/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.UnitTests.Extensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using static SonarLint.VisualStudio.ConnectedMode.BoundSolutionGitMonitor;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class BoundSolutionGitMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BoundSolutionGitMonitor, IBoundSolutionGitMonitor>(
                MefTestHelpers.CreateExport<IGitWorkspaceService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Initialize_NoRepo_FactoryNotCalledAndNoError()
        {
            var gitWorkspaceService = CreateGitWorkSpaceService(null);
            var factory = new Mock<GitEventFactory>();

            var testSubject = CreateTestSubject(gitWorkspaceService.Object, factory.Object);

            gitWorkspaceService.Verify(x => x.GetRepoRoot(), Times.Once);
            factory.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public void Initialize_ForwardsLowLevelEvent()
        {
            string repoPath = "some path";
            var gitWorkspaceService = CreateGitWorkSpaceService(repoPath);

            var gitEventsMonitor = new Mock<IGitEvents>();
            var factory = CreateFactory(gitEventsMonitor.Object);

            // first, check the factory is called
            BoundSolutionGitMonitor testSubject = CreateTestSubject(gitWorkspaceService.Object, factory.Object);
            factory.Verify(x => x.Invoke(repoPath), Times.Once);

            // second, register for then trigger an event
            int counter = 0;
            testSubject.HeadChanged += (o, e) => counter++;

            gitEventsMonitor.RaiseHeadChangedEvent();
            counter.Should().Be(1);
        }

        [TestMethod]
        public void Refresh_ChangesLowLevelMonitor()
        {
            string originalPath = "original path";
            string newPath = "new path";

            int counter = 0;

            var gitWorkspaceService = CreateGitWorkSpaceService(originalPath);

            var originalEventsMonitor = new Mock<IGitEvents>();
            var newEventsMonitor = new Mock<IGitEvents>();

            GitEventFactory gitEventFactory = (string path) =>
            {
                if (path != originalPath && path != newPath)
                {
                    throw new Exception("Test Error: Wrong path is passed to low level event monitor");
                }

                return path == originalPath ? originalEventsMonitor.Object : newEventsMonitor.Object;
            };

            BoundSolutionGitMonitor testSubject = CreateTestSubject(gitWorkspaceService.Object, gitEventFactory);
            testSubject.HeadChanged += (o, e) => counter++;

            newEventsMonitor.RaiseHeadChangedEvent();
            counter.Should().Be(0);

            originalEventsMonitor.RaiseHeadChangedEvent();
            counter.Should().Be(1);

            gitWorkspaceService.Setup(ws => ws.GetRepoRoot()).Returns(newPath);
            originalEventsMonitor.VerifyEventUnregistered(Times.Never);

            // Act
            testSubject.Refresh();

            // Old event handler should be unregistered
            originalEventsMonitor.VerifyEventUnregistered(Times.Once);
            originalEventsMonitor.RaiseHeadChangedEvent();
            counter.Should().Be(1);

            newEventsMonitor.RaiseHeadChangedEvent();
            counter.Should().Be(2);
        }

        [TestMethod]
        public void Dispose_UnregistersGitEventHandlerAndDisposesIGitEvents()
        {
            var gitWorkspaceService = CreateGitWorkSpaceService("any");

            var gitEvents = new Mock<IGitEvents>();
            var disposableGitEvents = gitEvents.As<IDisposable>();

            var factory = CreateFactory(gitEvents.Object);
            var testSubject = CreateTestSubject(gitWorkspaceService.Object, factory.Object);

            gitEvents.VerifyEventRegistered(Times.Once);
            gitEvents.VerifyEventUnregistered(Times.Never);
            disposableGitEvents.Verify(x => x.Dispose(), Times.Never);

            // Act
            testSubject.Dispose();

            gitEvents.VerifyEventRegistered(Times.Once); // still only once
            gitEvents.VerifyEventUnregistered(Times.Once); // unregistered once
            disposableGitEvents.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void OnHeadChanged_NonCriticalExceptionInHandler_IsSuppressed()
        {
            var gitWorkspaceService = CreateGitWorkSpaceService("any");

            var gitEvents = new Mock<IGitEvents>();
            var factory = CreateFactory(gitEvents.Object);
            var testSubject = CreateTestSubject(gitWorkspaceService.Object, factory.Object);

            testSubject.HeadChanged += (sender, args) => throw new InvalidOperationException("thrown from a test");

            Action op = gitEvents.RaiseHeadChangedEvent;

            op.Should().NotThrow();
        }

        [TestMethod]
        public void OnHeadChanged_CriticalExceptionInHandler_IsNotSuppressed()
        {
            var gitWorkspaceService = CreateGitWorkSpaceService("any");

            var gitEvents = new Mock<IGitEvents>();
            var factory = CreateFactory(gitEvents.Object);
            var testSubject = CreateTestSubject(gitWorkspaceService.Object, factory.Object);

            testSubject.HeadChanged += (sender, args) => throw new StackOverflowException("thrown from a test");

            Action op = gitEvents.RaiseHeadChangedEvent;

            op.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown from a test");
        }

        private Mock<IGitWorkspaceService> CreateGitWorkSpaceService(string repoPath)
        {
            var gitWorkspaceService = new Mock<IGitWorkspaceService>();
            gitWorkspaceService.Setup(ws => ws.GetRepoRoot()).Returns(repoPath);
            return gitWorkspaceService;
        }

        private static Mock<GitEventFactory> CreateFactory(IGitEvents gitEvents)
        {
            var factory = new Mock<GitEventFactory>();
            factory.Setup(x => x.Invoke(It.IsAny<string>())).Returns(gitEvents);
            return factory;
        }

        private BoundSolutionGitMonitor CreateTestSubject(IGitWorkspaceService gitWorkspaceService, GitEventFactory gitEventFactory)
            => new BoundSolutionGitMonitor(gitWorkspaceService, new TestLogger(logToConsole: true), gitEventFactory);
    }

    // Separate namespace so the extension methods don't pollute the main namespace
    namespace Extensions
    {
        internal static class BoundSolutionGitMonitorTestsExtensions
        {
            public static void RaiseHeadChangedEvent(this Mock<IGitEvents> gitEvents)
                => gitEvents.Raise(em => em.HeadChanged += null, null, null);

            public static void VerifyEventRegistered(this Mock<IGitEvents> gitEvents, Func<Times> times)
                => gitEvents.VerifyAdd(x => x.HeadChanged += It.IsAny<EventHandler>(), times);

            public static void VerifyEventUnregistered(this Mock<IGitEvents> gitEvents, Func<Times> times)
                => gitEvents.VerifyRemove(x => x.HeadChanged -= It.IsAny<EventHandler>(), times);
        }
    }

}
