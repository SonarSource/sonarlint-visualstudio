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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;
using EventHandler = System.EventHandler;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess
{
    [TestClass]
    public class RoslynSettingsFileSynchronizerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileSynchronizer, IRoslynSettingsFileSynchronizer>(null, new[]
            {
                MefTestHelpers.CreateExport<ISuppressedIssuesMonitor>(Mock.Of<ISuppressedIssuesMonitor>()),
                MefTestHelpers.CreateExport<ISonarQubeIssuesProvider>(Mock.Of<ISonarQubeIssuesProvider>()),
                MefTestHelpers.CreateExport<IRoslynSettingsFileStorage>(Mock.Of<IRoslynSettingsFileStorage>()),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(Mock.Of<IActiveSolutionBoundTracker>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
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
            var suppressedIssuesFileStorage = new Mock<IRoslynSettingsFileStorage>();
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(BindingConfiguration.Standalone);
            
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
            var suppressedIssuesFileStorage = new Mock<IRoslynSettingsFileStorage>();

            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var issues = hasIssues ? new[] { CreateSonarQubeIssue(), CreateSonarQubeIssue() } : Array.Empty<SonarQubeIssue>();
            var suppressedIssuesProvider = CreateSuppressedIssuesProvider(issues);

            CreateTestSubject(suppressedIssuesMonitor: suppressedIssuesMonitor.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object,
                suppressedIssuesFileStorage: suppressedIssuesFileStorage.Object,
                suppressedIssuesProvider: suppressedIssuesProvider.Object);

            suppressedIssuesMonitor.Raise(x => x.SuppressionsUpdateRequested += null, EventArgs.Empty);

            suppressedIssuesFileStorage.Verify(x => x.Update(It.IsAny<RoslynSettings>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateFileStorage_FileStorageIsUpdatedOnBackgroundThread()
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.SwitchToBackgroundThread()).Returns(new NoOpThreadHandler.NoOpAwaitable());

            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var suppressedIssuesFileStorage = new Mock<IRoslynSettingsFileStorage>();

            var issues = new[] { CreateSonarQubeIssue() };
            var suppressedIssuesProvider = CreateSuppressedIssuesProvider(issues);

            var testSubject = CreateTestSubject(
                threadHandling: threadHandling.Object,
                suppressedIssuesProvider: suppressedIssuesProvider.Object,
                suppressedIssuesFileStorage: suppressedIssuesFileStorage.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object);

            await testSubject.UpdateFileStorageAsync();

            threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);

            suppressedIssuesFileStorage.Invocations.Count.Should().Be(1);
            activeSolutionBoundTracker.Invocations.Count.Should().Be(1);
            suppressedIssuesFileStorage.Verify(x => x.Update(It.IsAny<RoslynSettings>()), Times.Once);
        }

        private static Mock<IActiveSolutionBoundTracker> CreateActiveSolutionBoundTracker(BindingConfiguration configuration)
        {
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(configuration);
            
            return activeSolutionBoundTracker;
        }

        private static Mock<ISonarQubeIssuesProvider> CreateSuppressedIssuesProvider(IEnumerable<SonarQubeIssue> issues)
        {
            var suppressedIssuesProvider = new Mock<ISonarQubeIssuesProvider>();
            suppressedIssuesProvider.Setup(x => x.GetAllSuppressedIssuesAsync()).Returns(Task.FromResult(issues));

            return suppressedIssuesProvider;
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

        private RoslynSettingsFileSynchronizer CreateTestSubject(ISuppressedIssuesMonitor suppressedIssuesMonitor = null,
            ISonarQubeIssuesProvider suppressedIssuesProvider = null,
            IRoslynSettingsFileStorage suppressedIssuesFileStorage = null,
            IActiveSolutionBoundTracker activeSolutionBoundTracker = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            suppressedIssuesMonitor ??= Mock.Of<ISuppressedIssuesMonitor>();
            suppressedIssuesProvider ??= Mock.Of<ISonarQubeIssuesProvider>();
            suppressedIssuesFileStorage ??= Mock.Of<IRoslynSettingsFileStorage>();
            activeSolutionBoundTracker ??= Mock.Of<IActiveSolutionBoundTracker>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= new TestLogger();

            return new RoslynSettingsFileSynchronizer(suppressedIssuesMonitor, 
                suppressedIssuesProvider, 
                suppressedIssuesFileStorage, 
                activeSolutionBoundTracker,
                logger,
                threadHandling);
        }
    }
}
