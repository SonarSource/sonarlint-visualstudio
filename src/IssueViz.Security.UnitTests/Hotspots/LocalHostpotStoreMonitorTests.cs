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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class LocalHostpotStoreMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<LocalHotspotStoreMonitor, ILocalHotspotStoreMonitor>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILocalHotspotsStore>(),
                MefTestHelpers.CreateExport<IThreadHandling>());

        [TestMethod]
        public void CheckIsSingletonMefComponent()
            => MefTestHelpers.CheckIsSingletonMefComponent<LocalHotspotStoreMonitor>();

        [TestMethod]
        public void Ctor_DoesNotListenToEvents()
        {
            var serviceProvider = CreateServiceProvider();
            var store = CreateStore();

            _ = CreateTestSubject(serviceProvider.Object, store.Object);

            // ctor should do nothing except set fields
            serviceProvider.Invocations.Count.Should().Be(0);

            RaiseIssuesChanged(store);
            store.Invocations.Count.Should().Be(0);
            serviceProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task InitializeAsync_FetchesCookieAndCallsRefresh()
        {
            var monitorSelection = CreateMonitorSelection(123);
            var serviceProvider = CreateServiceProvider(monitorSelection.Object);

            var store = CreateStore();

            var testSubject = CreateTestSubject(serviceProvider.Object, store.Object);

            await testSubject.InitializeAsync();

            CheckUIContextCookieIsFetched(monitorSelection, 123);
            CheckUIContextIsCleared(monitorSelection, 123);
        }

        [TestMethod]
        public async Task IssuesChanged_HasIssues_ContextIsSet()
        {
            var monitorSelection = CreateMonitorSelection(222);
            var serviceProvider = CreateServiceProvider(monitorSelection.Object);
            var store = CreateStore(new LocalHotspot(Mock.Of<IAnalysisIssueVisualization>(), Security.Hotspots.Models.HotspotPriority.Medium));

            _ = await CreateInitializedTestSubject(serviceProvider.Object, store.Object);
            monitorSelection.Invocations.Clear();

            RaiseIssuesChanged(store);
            CheckUIContextIsSet(monitorSelection, 222);
        }

        [TestMethod]
        public async Task IssuesChanged_NoIssues_ContextIsCleared()
        {
            var monitorSelection = CreateMonitorSelection(333);
            var serviceProvider = CreateServiceProvider(monitorSelection.Object);
            var store = CreateStore(/* no issues*/);

            _ = await CreateInitializedTestSubject(serviceProvider.Object, store.Object);
            monitorSelection.Invocations.Clear();

            RaiseIssuesChanged(store);
            CheckUIContextIsCleared(monitorSelection, 333);
        }

        [TestMethod]
        public async Task InitializeAsync_InitializedOnUIThread()
        {
            var callSequence = new List<string>();

            var store = CreateStore();

            var monitorSelection = CreateMonitorSelection(333,
                () => callSequence.Add("GetCmdUICookieContext"),
                () => callSequence.Add("SetCmdUIContext"));

            var serviceProvider = CreateServiceProvider(monitorSelection.Object);

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling
                .Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback((Action callbackAction) =>
                {
                    callSequence.Add("RunOnUIThread");
                    callbackAction();
                });

            // Ctor
            var testSubject = CreateTestSubject(serviceProvider.Object, store.Object, threadHandling.Object);
            callSequence.Should().BeEmpty();

            // Initialize
            await testSubject.InitializeAsync();

            callSequence.Should().ContainInOrder("RunOnUIThread",
                "SetCmdUIContext");
        }

        [TestMethod]
        public void IssuesChanged_ContextIsUpdatedOnUIThread()
        {
            var callSequence = new List<string>();

            var store = CreateStore();

            var monitorSelection = CreateMonitorSelection(333,
                null,
                () => callSequence.Add("SetCmdUIContext"));
            var serviceProvider = CreateServiceProvider(monitorSelection.Object);


            var threadHandling = new Mock<IThreadHandling>();
            threadHandling
                .Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback((Action callbackAction) =>
                {
                    callSequence.Add("RunOnUIThread");
                    callbackAction();
                });

            // Create and initialize
            _ = CreateInitializedTestSubject(serviceProvider.Object, store.Object, threadHandling.Object);
            callSequence.Clear();

            RaiseIssuesChanged(store);

            callSequence.Should().ContainInOrder("RunOnUIThread",
                "SetCmdUIContext");
        }

        [TestMethod]
        public void Dispose_UnregistersEvent()
        {
            var store = CreateStore();

            var testSubject = CreateTestSubject(localHotspotsStore: store.Object);

            testSubject.Dispose();

            store.VerifyRemove(x => x.IssuesChanged -= It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once);
        }

        private static LocalHotspotStoreMonitor CreateTestSubject(IServiceProvider serviceProvider = null,
            ILocalHotspotsStore localHotspotsStore = null,
            IThreadHandling threadHandling = null)
        {
            return new LocalHotspotStoreMonitor(serviceProvider ?? Mock.Of<IServiceProvider>(),
                localHotspotsStore ?? Mock.Of<ILocalHotspotsStore>(),
                threadHandling ?? new NoOpThreadHandler());
        }

        private async static Task<LocalHotspotStoreMonitor> CreateInitializedTestSubject(IServiceProvider serviceProvider = null,
            ILocalHotspotsStore localHotspotsStore = null,
            IThreadHandling threadHandling = null)
        {
            // The LocalHotspotStoreMonitor needs to be initialized before it tracks events
            var testSubject = CreateTestSubject(serviceProvider, localHotspotsStore, threadHandling );
            await testSubject.InitializeAsync();
            return testSubject;
        }

        private static Mock<IVsMonitorSelection> CreateMonitorSelection(uint cookieToReturn,
            Action getCmdUICallback = null,
            Action setCmdUICallback = null)
        {
            var monitor = new Mock<IVsMonitorSelection>();
            var localGuid = LocalHotspotIssuesExistUIContext.Guid;
            monitor.Setup(x => x.GetCmdUIContextCookie(ref localGuid, out cookieToReturn))
                .Callback(() => getCmdUICallback?.Invoke());

            if (setCmdUICallback != null)
            {
                monitor.Setup(x => x.SetCmdUIContext(It.IsAny<uint>(), It.IsAny<int>()))
                    .Callback(() => setCmdUICallback());
            }

            return monitor;
        }

        private static Mock<IServiceProvider> CreateServiceProvider(IVsMonitorSelection vsMonitor = null,
            Action callback = null)
        {
            vsMonitor ??= Mock.Of<IVsMonitorSelection>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(vsMonitor).
                Callback(() => callback?.Invoke());
            return serviceProvider;
        }

        private static Mock<ILocalHotspotsStore> CreateStore(params LocalHotspot[] hotspotsToReturn)
        {
            var store = new Mock<ILocalHotspotsStore>();
            store.Setup(x => x.GetAllLocalHotspots()).Returns(new List<LocalHotspot>(hotspotsToReturn));
            return store;
        }

        private static void RaiseIssuesChanged(Mock<ILocalHotspotsStore> store)
            => store.Raise(x => x.IssuesChanged += null, null, new IssuesChangedEventArgs(null, null));

        private static void CheckUIContextCookieIsFetched(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie)
        {
            var localGuid = LocalHotspotIssuesExistUIContext.Guid;
            monitorMock.Verify(x => x.GetCmdUIContextCookie(ref localGuid, out expectedCookie), Times.Once);
        }

        private static void CheckUIContextIsCleared(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie) =>
            CheckUIContextUpdated(monitorMock, expectedCookie, 0);

        private static void CheckUIContextIsSet(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie) =>
            CheckUIContextUpdated(monitorMock, expectedCookie, 1);

        private static void CheckUIContextUpdated(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie, int expectedState) =>
            monitorMock.Verify(x => x.SetCmdUIContext(expectedCookie, expectedState), Times.Once);
    }
}
