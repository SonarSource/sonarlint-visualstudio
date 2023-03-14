﻿/*
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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Roslyn.Suppressions.InProcess;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;
using EventHandler = System.EventHandler;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;
using System.Linq;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.InProcess
{
    [TestClass]
    public class RoslynSettingsFileSynchronizerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileSynchronizer, IRoslynSettingsFileSynchronizer>(
                MefTestHelpers.CreateExport<IServerIssuesStore>(),
                MefTestHelpers.CreateExport<IRoslynSettingsFileStorage>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Ctor_RegisterToSuppressionsUpdateRequestedEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            serverIssuesStore.SetupAdd(x => x.ServerIssuesChanged += null);

            CreateTestSubject(serverIssuesStore: serverIssuesStore.Object);

            serverIssuesStore.VerifyAdd(x => x.ServerIssuesChanged += It.IsAny<EventHandler>(), Times.Once());
            serverIssuesStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSuppressionsUpdateRequestedEvent()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();

            serverIssuesStore.SetupAdd(x => x.ServerIssuesChanged += null);
            serverIssuesStore.SetupRemove(x => x.ServerIssuesChanged -= null);

            var testSubject = CreateTestSubject(serverIssuesStore: serverIssuesStore.Object);

            serverIssuesStore.VerifyAdd(x => x.ServerIssuesChanged += It.IsAny<EventHandler>(), Times.Once());
            serverIssuesStore.VerifyNoOtherCalls();

            testSubject.Dispose();

            serverIssuesStore.VerifyRemove(x => x.ServerIssuesChanged -= It.IsAny<EventHandler>(), Times.Once());
            serverIssuesStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnSuppressionsUpdateRequested_StandaloneMode_StorageNotUpdated()
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();
            var roslynSettingsFileStorage = new Mock<IRoslynSettingsFileStorage>();
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(BindingConfiguration.Standalone);
            
            CreateTestSubject(serverIssuesStore: serverIssuesStore.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object,
                roslynSettingsFileStorage: roslynSettingsFileStorage.Object);

            serverIssuesStore.Raise(x=> x.ServerIssuesChanged += null, EventArgs.Empty);

            activeSolutionBoundTracker.Verify(x=> x.CurrentConfiguration, Times.Once);
            roslynSettingsFileStorage.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)] // should update storage even when there are no issues
        public void OnSuppressionsUpdateRequested_ConnectedMode_StorageUpdated(bool hasIssues)
        {
            var roslynSettingsFileStoragerage = new Mock<IRoslynSettingsFileStorage>();

            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var issues = hasIssues ? new[] { CreateSonarQubeIssue(), CreateSonarQubeIssue() } : Array.Empty<SonarQubeIssue>();
            var serverIssuesStore = CreateServerIssuesStore(issues);

            CreateTestSubject(serverIssuesStore: serverIssuesStore.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object,
                roslynSettingsFileStorage: roslynSettingsFileStoragerage.Object);

            serverIssuesStore.Raise(x => x.ServerIssuesChanged += null, EventArgs.Empty);

            roslynSettingsFileStoragerage.Verify(x => x.Update(It.IsAny<RoslynSettings>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateFileStorage_FileStorageIsUpdatedOnBackgroundThread()
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.SwitchToBackgroundThread()).Returns(new NoOpThreadHandler.NoOpAwaitable());

            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var roslynSettingsFileStorage = new Mock<IRoslynSettingsFileStorage>();

            var issues = new[] { CreateSonarQubeIssue() };
            var serverIssuesStore = CreateServerIssuesStore(issues);

            var testSubject = CreateTestSubject(
                serverIssuesStore: serverIssuesStore.Object,
                threadHandling: threadHandling.Object,
                roslynSettingsFileStorage: roslynSettingsFileStorage.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object);

            await testSubject.UpdateFileStorageAsync();

            threadHandling.Verify(x => x.SwitchToBackgroundThread(), Times.Once);

            roslynSettingsFileStorage.Invocations.Count.Should().Be(1);
            activeSolutionBoundTracker.Invocations.Count.Should().Be(1);
            roslynSettingsFileStorage.Verify(x => x.Update(It.IsAny<RoslynSettings>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateFileStorage_IssuesAreConvertedAndFiltered()
        {
            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var sonarIssues = new []
            {
                CreateSonarQubeIssue(ruleId: "csharpsquid:S111"), // C# issue
                CreateSonarQubeIssue(ruleId: "vbnet:S222"),// VB issue
                CreateSonarQubeIssue(ruleId: "cpp:S333"),// C++ issue - ignored
                CreateSonarQubeIssue(ruleId: "xxx:S444"),// unrecognised repo - ignored
                CreateSonarQubeIssue(ruleId: "xxxS555"),// invalid repo key - ignored
                CreateSonarQubeIssue(ruleId: "xxx:"),// invalid repo key (no rule id) - ignored
            };

            var serverIssuesStore = CreateServerIssuesStore(sonarIssues);

            RoslynSettings actualSettings = null;
            var roslynSettingsFileStorage = new Mock<IRoslynSettingsFileStorage>();
            roslynSettingsFileStorage.Setup(x => x.Update(It.IsAny<RoslynSettings>()))
                .Callback<RoslynSettings>(x => actualSettings = x);

            var testSubject = CreateTestSubject(
                serverIssuesStore: serverIssuesStore.Object,
                roslynSettingsFileStorage: roslynSettingsFileStorage.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object);

            await testSubject.UpdateFileStorageAsync();

            actualSettings.Should().NotBeNull();
            actualSettings.SonarProjectKey.Should().Be("some project key");
            actualSettings.Suppressions.Should().NotBeNull();

            var actualSuppressions = actualSettings.Suppressions.ToList();
            actualSuppressions.Count.Should().Be(2);
            actualSuppressions[0].RoslynLanguage.Should().Be(RoslynLanguage.CSharp);
            actualSuppressions[0].RoslynRuleId.Should().Be("S111");
            actualSuppressions[1].RoslynLanguage.Should().Be(RoslynLanguage.VB);
            actualSuppressions[1].RoslynRuleId.Should().Be("S222");
        }

        [TestMethod]
        public async Task UpdateFileStorage_OnySuppressedIssuesAreInSettings()
        {
            var configuration = CreateConnectedConfiguration("some project key");
            var activeSolutionBoundTracker = CreateActiveSolutionBoundTracker(configuration);

            var sonarIssues = new[]
            {
                CreateSonarQubeIssue(ruleId: "csharpsquid:S111", isSuppressed:false),
                CreateSonarQubeIssue(ruleId: "vbnet:S222", isSuppressed:true),
                CreateSonarQubeIssue(ruleId: "csharpsquid:S333", isSuppressed:true),
                CreateSonarQubeIssue(ruleId: "vbnet:S444", isSuppressed:false),
            };

            var serverIssuesStore = CreateServerIssuesStore(sonarIssues);

            RoslynSettings actualSettings = null;
            var roslynSettingsFileStorage = new Mock<IRoslynSettingsFileStorage>();
            roslynSettingsFileStorage.Setup(x => x.Update(It.IsAny<RoslynSettings>()))
                .Callback<RoslynSettings>(x => actualSettings = x);

            var testSubject = CreateTestSubject(
                serverIssuesStore: serverIssuesStore.Object,
                roslynSettingsFileStorage: roslynSettingsFileStorage.Object,
                activeSolutionBoundTracker: activeSolutionBoundTracker.Object);

            await testSubject.UpdateFileStorageAsync();

            actualSettings.Should().NotBeNull();
            actualSettings.SonarProjectKey.Should().Be("some project key");
            actualSettings.Suppressions.Should().NotBeNull();

            var actualSuppressions = actualSettings.Suppressions.ToList();
            actualSuppressions.Count.Should().Be(2);
            actualSuppressions[0].RoslynLanguage.Should().Be(RoslynLanguage.VB);
            actualSuppressions[0].RoslynRuleId.Should().Be("S222");
            actualSuppressions[1].RoslynLanguage.Should().Be(RoslynLanguage.CSharp);
            actualSuppressions[1].RoslynRuleId.Should().Be("S333");
        }

        private static Mock<IActiveSolutionBoundTracker> CreateActiveSolutionBoundTracker(BindingConfiguration configuration)
        {
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.Setup(x => x.CurrentConfiguration).Returns(configuration);
            
            return activeSolutionBoundTracker;
        }

        private static Mock<IServerIssuesStore> CreateServerIssuesStore(IEnumerable<SonarQubeIssue> issues = null)
        {
            var serverIssuesStore = new Mock<IServerIssuesStore>();
            serverIssuesStore.Setup(x => x.Get()).Returns(issues);

            return serverIssuesStore;
        }


        private static BindingConfiguration CreateConnectedConfiguration(string projectKey)
        {
            var project = new BoundSonarQubeProject(new Uri("http://localhost"), projectKey, "project name");
            
            return BindingConfiguration.CreateBoundConfiguration(project, SonarLintMode.Connected, "some directory");
        }

        private RoslynSettingsFileSynchronizer CreateTestSubject(IServerIssuesStore serverIssuesStore = null,
            IRoslynSettingsFileStorage roslynSettingsFileStorage = null,
            IActiveSolutionBoundTracker activeSolutionBoundTracker = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            serverIssuesStore ??= Mock.Of<IServerIssuesStore>();
            roslynSettingsFileStorage ??= Mock.Of<IRoslynSettingsFileStorage>();
            activeSolutionBoundTracker ??= Mock.Of<IActiveSolutionBoundTracker>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= new TestLogger();

            return new RoslynSettingsFileSynchronizer(serverIssuesStore,
                roslynSettingsFileStorage, 
                activeSolutionBoundTracker,
                logger,
                threadHandling);
        }
    }
}
