/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarQube.Client.Models;
using EventHandler = System.EventHandler;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess
{
    [TestClass]
    public class SuppressedIssuesFileSynchronizerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SuppressedIssuesFileSynchronizer, ISuppressedIssuesFileSynchronizer>(null, new[]
            {
                MefTestHelpers.CreateExport<ISuppressedIssuesMonitor>(Mock.Of<ISuppressedIssuesMonitor>()),
                MefTestHelpers.CreateExport<ISonarQubeIssuesProvider>(Mock.Of<ISonarQubeIssuesProvider>()),
                MefTestHelpers.CreateExport<ISuppressedIssuesFileStorage>(Mock.Of<ISuppressedIssuesFileStorage>()),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(Mock.Of<IActiveSolutionBoundTracker>())
            });
        }

        [TestMethod]
        public void Ctor_RegisterToSuppressionsUpdateRequestedEvent()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();

            suppressedIssuesMonitor.SetupAdd(x => x.SuppressionsUpdateRequested += null);

            CreateTestSubject(suppressedIssuesMonitor: suppressedIssuesMonitor.Object);

            suppressedIssuesMonitor.VerifyAdd(x => x.SuppressionsUpdateRequested += It.IsAny<EventHandler>(), Times.Once());
            suppressedIssuesMonitor.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSuppressionsUpdateRequestedEvent()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();

            suppressedIssuesMonitor.SetupAdd(x => x.SuppressionsUpdateRequested += null);
            suppressedIssuesMonitor.SetupRemove(x => x.SuppressionsUpdateRequested -= null);

            var testSubject = CreateTestSubject(suppressedIssuesMonitor: suppressedIssuesMonitor.Object);

            suppressedIssuesMonitor.VerifyAdd(x => x.SuppressionsUpdateRequested += It.IsAny<EventHandler>(), Times.Once());
            suppressedIssuesMonitor.VerifyNoOtherCalls();

            testSubject.Dispose();

            suppressedIssuesMonitor.VerifyRemove(x => x.SuppressionsUpdateRequested -= It.IsAny<EventHandler>(), Times.Once());
            suppressedIssuesMonitor.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnSuppressionsUpdateRequested_StandaloneMode_StorageNotUpdated()
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var suppressedIssuesFileStorage = new Mock<ISuppressedIssuesFileStorage>();

            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(BindingConfiguration.Standalone);

            CreateTestSubject(suppressedIssuesMonitor: suppressedIssuesMonitor.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object,
                suppressedIssuesFileStorage: suppressedIssuesFileStorage.Object);

            suppressedIssuesMonitor.Raise(x=> x.SuppressionsUpdateRequested += null, EventArgs.Empty);

            activeSolutionBoundTracker.Verify(x=> x.CurrentConfiguration, Times.Once);
            suppressedIssuesFileStorage.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)] // should update storage even when there are no issues
        public void OnSuppressionsUpdateRequested_ConnectedMode_StorageUpdated(bool hasIssues)
        {
            var suppressedIssuesMonitor = new Mock<ISuppressedIssuesMonitor>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var suppressedIssuesFileStorage = new Mock<ISuppressedIssuesFileStorage>();
            var suppressedIssuesProvider = new Mock<ISonarQubeIssuesProvider>();

            var configuration = CreateConnectedConfiguration("some project key");
            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(configuration);

            var issues = hasIssues ? new[] { CreateSonarQubeIssue(), CreateSonarQubeIssue() } : Array.Empty<SonarQubeIssue>();
            suppressedIssuesProvider.Setup(x => x.GetAllSuppressedIssues()).Returns(issues);

            CreateTestSubject(suppressedIssuesMonitor: suppressedIssuesMonitor.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object,
                suppressedIssuesFileStorage: suppressedIssuesFileStorage.Object,
                suppressedIssuesProvider: suppressedIssuesProvider.Object);

            suppressedIssuesMonitor.Raise(x => x.SuppressionsUpdateRequested += null, EventArgs.Empty);

            suppressedIssuesFileStorage.Verify(x=> x.Update("some project key", issues), Times.Once);
        }

        [TestMethod]
        public async Task UpdateFileStorage_FileStorageIsUpdatedOnBackgroundThread()
        {
            Func<Task<bool>> fileUpdateTask = null;
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling
                .Setup(x => x.Run(It.IsAny<Func<Task<bool>>>()))
                .Callback((Func<Task<bool>> task) => fileUpdateTask = task);

            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(configuration);

            var suppressedIssuesFileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var issues = new[] { CreateSonarQubeIssue() };

            var suppressedIssuesProvider = new Mock<ISonarQubeIssuesProvider>();
            suppressedIssuesProvider.Setup(x => x.GetAllSuppressedIssues()).Returns(issues);

            var testSubject = CreateTestSubject(
                threadHandling: threadHandling.Object,
                suppressedIssuesProvider: suppressedIssuesProvider.Object,
                suppressedIssuesFileStorage: suppressedIssuesFileStorage.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object);

            testSubject.UpdateFileStorage();

            threadHandling.Verify(x => x.Run(It.IsAny<Func<Task<bool>>>()), Times.Once);
            suppressedIssuesFileStorage.Invocations.Count.Should().Be(0);
            activeSolutionBoundTracker.Invocations.Count.Should().Be(0);

            await fileUpdateTask();

            suppressedIssuesFileStorage.Verify(x => x.Update("some project key", issues), Times.Once);
        }

        private SonarQubeIssue CreateSonarQubeIssue()
        {
            return new SonarQubeIssue(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), false,
                SonarQubeIssueSeverity.Blocker, new DateTimeOffset(), new DateTimeOffset(), null, null);
        }

        private static BindingConfiguration CreateConnectedConfiguration(string projectKey)
        {
            var project = new BoundSonarQubeProject(new Uri("http://localhost"), projectKey, "project name");
            
            return BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, "some directory");
        }

        private SuppressedIssuesFileSynchronizer CreateTestSubject(ISuppressedIssuesMonitor suppressedIssuesMonitor = null,
            ISonarQubeIssuesProvider suppressedIssuesProvider = null,
            ISuppressedIssuesFileStorage suppressedIssuesFileStorage = null,
            IActiveSolutionBoundTracker activeSolutionBoundTracker = null,
            IThreadHandling threadHandling = null)
        {
            suppressedIssuesMonitor ??= Mock.Of<ISuppressedIssuesMonitor>();
            suppressedIssuesProvider ??= Mock.Of<ISonarQubeIssuesProvider>();
            suppressedIssuesFileStorage ??= Mock.Of<ISuppressedIssuesFileStorage>();
            activeSolutionBoundTracker ??= Mock.Of<IActiveSolutionBoundTracker>();
            threadHandling ??= new NoOpThreadHandler();

            return new SuppressedIssuesFileSynchronizer(suppressedIssuesMonitor, 
                suppressedIssuesProvider, 
                suppressedIssuesFileStorage, 
                activeSolutionBoundTracker,
                threadHandling);
        }
    }
}
