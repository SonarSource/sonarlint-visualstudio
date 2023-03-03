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
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressedIssuesProviderTests
    {
        private Mock<SuppressedIssuesProvider.CreateProviderFunc> createProviderFunc;
        private ConfigurableActiveSolutionBoundTracker activeSolutionBoundTracker;

        private SuppressedIssuesProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            createProviderFunc = new Mock<SuppressedIssuesProvider.CreateProviderFunc>();
            activeSolutionBoundTracker = new ConfigurableActiveSolutionBoundTracker();
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;

            testSubject = new SuppressedIssuesProvider(activeSolutionBoundTracker, createProviderFunc.Object);
        }

        [TestMethod]
        public void MefCtor_TwoExports_Singleton()
        {
            // SuppressIssuesProvider is unusual as it exports two types via MEF.
            // We expect both types to be exportable, and for them to return the
            // same instance.

            var providerImporter = new SingleObjectImporter<ISonarQubeIssuesProvider>();
            var monitorImporter = new SingleObjectImporter<ISuppressedIssuesMonitor>();

            var exports = GetRequiredMefDependencies();

            MefTestHelpers.Compose(
                new object[] { providerImporter, monitorImporter },
                new Type[] { typeof(SuppressedIssuesProvider) },
                exports);

            providerImporter.Import.Should().NotBeNull();
            monitorImporter.Import.Should().NotBeNull();

            providerImporter.Import.Should().BeSameAs(monitorImporter.Import);
        }

        private static Export[] GetRequiredMefDependencies()
        {
            // The constructor executes some initialization code, so we need
            // to provide a valid configuration to prevent it throwing.
            var tracker = new Mock<IActiveSolutionBoundTracker>();
            tracker.Setup(x => x.CurrentConfiguration).Returns(BindingConfiguration.Standalone);

            return new[]
            {
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(tracker.Object),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
                MefTestHelpers.CreateExport<ILogger>()
            };
        }

        [TestMethod]
        public void Ctor_NullCreateProviderFunc_ArgumentNullException()
        {
            Action act = () => new SuppressedIssuesProvider(activeSolutionBoundTracker, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("createProviderFunc");
        }

        [TestMethod]
        public void Ctor_NullActiveSolutionBoundTracker_ArgumentNullException()
        {
            Action act = () => new SuppressedIssuesProvider(null, createProviderFunc.Object);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeSolutionBoundTracker");
        }

        [TestMethod]
        public void Ctor_SolutionBindingIsStandalone_SonarQubeIssuesProviderNotCreated()
        {
            activeSolutionBoundTracker.CurrentConfiguration = BindingConfiguration.Standalone;
            new SuppressedIssuesProvider(activeSolutionBoundTracker, createProviderFunc.Object);

            createProviderFunc.VerifyNoOtherCalls();
        }

        [DataTestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public void Ctor_SolutionBindingIsConnected_SonarQubeIssuesProviderCreated(SonarLintMode mode)
        {
            activeSolutionBoundTracker.CurrentConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), mode, null);
            new SuppressedIssuesProvider(activeSolutionBoundTracker, createProviderFunc.Object);

            createProviderFunc.Verify(x=> x(activeSolutionBoundTracker.CurrentConfiguration), Times.Once);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public void GetSuppressedIssues_SolutionBindingIsStandalone_EmptyList(SolutionBindingEventType eventType)
        {
            SimulateBindingEvent(eventType, BindingConfiguration.Standalone);

            var actual = testSubject.GetSuppressedIssues("project guid", "file path");

            actual.Should().BeEmpty();
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public void GetSuppressedIssues_SolutionBindingIsStandalone_SonarQubeIssuesProviderNotCreated(SolutionBindingEventType eventType)
        {
            SimulateBindingEvent(eventType, BindingConfiguration.Standalone);

            testSubject.GetSuppressedIssues("project guid", "file path");

            createProviderFunc.Verify(x =>
                    x(It.IsAny<BindingConfiguration>()),
                Times.Never);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.LegacyConnected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.LegacyConnected)]
        public void GetSuppressedIssues_SolutionBindingIsConnected_ListFromSonarQubeIssuesProvider(SolutionBindingEventType eventType, SonarLintMode mode)
        {
            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), mode, null);
            var expectedIssues = SetupExpectedIssues(bindingConfiguration);

            SimulateBindingEvent(eventType, bindingConfiguration);

            var actual = testSubject.GetSuppressedIssues("project guid", "file path");

            actual.Should().BeEquivalentTo(expectedIssues);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public async Task GetAllSuppressedIssues_SolutionBindingIsStandalone_EmptyList(SolutionBindingEventType eventType)
        {
            SimulateBindingEvent(eventType, BindingConfiguration.Standalone);

            var actual = await testSubject.GetAllSuppressedIssuesAsync();

            actual.Should().BeEmpty();
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public async Task GetAllSuppressedIssues_SolutionBindingIsStandalone_SonarQubeIssuesProviderNotCreated(SolutionBindingEventType eventType)
        {
            SimulateBindingEvent(eventType, BindingConfiguration.Standalone);

            await testSubject.GetAllSuppressedIssuesAsync();

            createProviderFunc.Verify(x =>
                    x(It.IsAny<BindingConfiguration>()),
                Times.Never);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.LegacyConnected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.LegacyConnected)]
        public async Task GetAllSuppressedIssues_SolutionBindingIsConnected_ListFromSonarQubeIssuesProvider(SolutionBindingEventType eventType, SonarLintMode mode)
        {
            var bindingConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), mode, null);
            var expectedIssues = SetupExpectedIssues(bindingConfiguration);

            SimulateBindingEvent(eventType, bindingConfiguration);

            var actual = await testSubject.GetAllSuppressedIssuesAsync();

            actual.Should().BeEquivalentTo(expectedIssues);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public void SolutionBindingEvent_Standalone_SuppressionsEventNotRaised(SolutionBindingEventType eventType)
        {
            var eventMock = new Mock<EventHandler>();
            testSubject.ServerSuppressionsChanged += eventMock.Object;

            var configuration = new BindingConfiguration(new BoundSonarQubeProject(), SonarLintMode.Standalone, null);
            SimulateBindingEvent(eventType, configuration);

            eventMock.VerifyNoOtherCalls();
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged)]
        public void SolutionBindingEvent_Standalone_SonarQubeIssuesProviderNotCreated(SolutionBindingEventType eventType)
        {
            var configuration = new BindingConfiguration(new BoundSonarQubeProject(), SonarLintMode.Standalone, null);
            SimulateBindingEvent(eventType, configuration);

            createProviderFunc.VerifyNoOtherCalls();
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.LegacyConnected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.LegacyConnected)]
        public void SolutionBindingEvent_Connected_SuppressionsEventRaised(SolutionBindingEventType eventType, SonarLintMode mode)
        {
            var eventMock = new Mock<EventHandler>();
            testSubject.ServerSuppressionsChanged += eventMock.Object;

            var configuration = new BindingConfiguration(new BoundSonarQubeProject(), mode, null);
            SimulateBindingEvent(eventType, configuration);

            eventMock.Verify(x=> x(testSubject, EventArgs.Empty), Times.Once);
        }

        [DataTestMethod]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.Standalone)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.Standalone)]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.Connected)]
        [DataRow(SolutionBindingEventType.SolutionBindingUpdated, SonarLintMode.LegacyConnected)]
        [DataRow(SolutionBindingEventType.SolutionBindingChanged, SonarLintMode.LegacyConnected)]
        public void SolutionBindingEvent_PreviousProviderIsDisposed(SolutionBindingEventType eventType, SonarLintMode mode)
        {
            var firstProvider = new Mock<ISonarQubeIssuesProvider>();
            var firstConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), SonarLintMode.Connected, null);

            createProviderFunc
                .Setup(x => x(firstConfiguration))
                .Returns(firstProvider.Object);

            SimulateBindingEvent(eventType, firstConfiguration);

            firstProvider.Verify(x=> x.Dispose(), Times.Never);

            var secondConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), mode, null);

            SimulateBindingEvent(eventType, secondConfiguration);

            firstProvider.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_ProviderExists_ProviderIsDisposed()
        {
            var firstProvider = new Mock<ISonarQubeIssuesProvider>();
            var firstConfiguration = new BindingConfiguration(new BoundSonarQubeProject(), SonarLintMode.Connected, null);

            createProviderFunc
                .Setup(x => x(firstConfiguration))
                .Returns(firstProvider.Object);

            SimulateBindingEvent(SolutionBindingEventType.SolutionBindingChanged, firstConfiguration);

            firstProvider.Verify(x => x.Dispose(), Times.Never);

            testSubject.Dispose();

            firstProvider.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void Dispose_EventsUnsubscribed()
        {
            var changedInvocationList = activeSolutionBoundTracker.GetSolutionBindingChangedInvocationList();
            var updatedInvocationList = activeSolutionBoundTracker.GetSolutionBindingUpdatedInvocationList();

            changedInvocationList.Should().NotBeEmpty();
            updatedInvocationList.Should().NotBeEmpty();

            testSubject.Dispose();

            changedInvocationList = activeSolutionBoundTracker.GetSolutionBindingChangedInvocationList();
            updatedInvocationList = activeSolutionBoundTracker.GetSolutionBindingUpdatedInvocationList();

            changedInvocationList.Should().BeNullOrEmpty();
            updatedInvocationList.Should().BeNullOrEmpty();
        }

        private void SimulateBindingEvent(SolutionBindingEventType eventType, BindingConfiguration newConfiguration)
        {
            if (eventType == SolutionBindingEventType.SolutionBindingUpdated)
            {
                activeSolutionBoundTracker.CurrentConfiguration = newConfiguration;
                activeSolutionBoundTracker.SimulateSolutionBindingUpdated();
            }
            else
            {
                activeSolutionBoundTracker.SimulateSolutionBindingChanged(
                    new ActiveSolutionBindingEventArgs(newConfiguration));
            }
        }

        private IEnumerable<SonarQubeIssue> SetupExpectedIssues(BindingConfiguration bindingConfiguration)
        {
            IEnumerable<SonarQubeIssue> expectedIssues = new List<SonarQubeIssue>
            {
                new SonarQubeIssue("id1", "file path", "hash", "message", "module", "rule id", true, SonarQubeIssueSeverity.Critical,
                    DateTimeOffset.MinValue, DateTimeOffset.MinValue, new IssueTextRange(1, 2, 3, 4),  flows: null),

                new SonarQubeIssue("id2", "file path2", "hash2", "message2", "module2", "rule id2", false, SonarQubeIssueSeverity.Critical,
                    DateTimeOffset.MinValue, DateTimeOffset.MinValue, new IssueTextRange(2, 3, 4, 5),   flows: null)
            };

            var issuesProvider = new Mock<ISonarQubeIssuesProvider>();
            issuesProvider
                .Setup(x => x.GetSuppressedIssues("project guid", "file path"))
                .Returns(expectedIssues);

            issuesProvider
                .Setup(x => x.GetAllSuppressedIssuesAsync())
                .Returns(Task.FromResult(expectedIssues));

            createProviderFunc
                .Setup(x => x(bindingConfiguration))
                .Returns(issuesProvider.Object);

            return expectedIssues;
        }

        public enum SolutionBindingEventType
        {
            SolutionBindingUpdated,
            SolutionBindingChanged
        }
    }
}
