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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class ServerHotspotStoreTests
    {
        [TestMethod]
        public async Task UpdateAsync_NoRecordFound_CallsService()
        {
            var projectKey = nameof(UpdateAsync_NoRecordFound_CallsService);
            var branchName = "main";
            var compositeKey = ServerHotspotStore.CreateCompositeKey(projectKey, branchName);

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = CreateTestSubject(sonarQubeService.Object);
            var eventCounter = new EventCounter(testSubject);

            //Check if the test in correct state
            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeFalse();

            await testSubject.UpdateAsync(projectKey, branchName, CancellationToken.None);

            sonarQubeService.Verify(s => s.SearchHotspotsAsync(projectKey, branchName, It.IsAny<CancellationToken>()), Times.Once);
            sonarQubeService.VerifyNoOtherCalls();

            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeTrue();

            eventCounter.Events.Should().HaveCount(1);
            eventCounter.Events[0].ProjectKey.Should().Be(projectKey);
            eventCounter.Events[0].BranchName.Should().Be(branchName);
        }

        [TestMethod]
        public async Task UpdateAsync_RecordFound_CallsService()
        {
            var projectKey = nameof(UpdateAsync_RecordFound_CallsService);
            var branchName = "main";
            var compositeKey = ServerHotspotStore.CreateCompositeKey(projectKey, branchName);

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = new ServerHotspotStore(sonarQubeService.Object);
            var eventCounter = new EventCounter(testSubject);

            ServerHotspotStore.serverHotspots.Add(compositeKey, new List<SonarQubeHotspotSearch>());

            //Check if the test in correct state
            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeTrue();
            ServerHotspotStore.serverHotspots[compositeKey].Should().NotBeNull();

            await testSubject.UpdateAsync(projectKey, branchName, CancellationToken.None);

            sonarQubeService.Verify(s => s.SearchHotspotsAsync(projectKey, branchName, It.IsAny<CancellationToken>()), Times.Once);
            sonarQubeService.VerifyNoOtherCalls();

            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeTrue();
            ServerHotspotStore.serverHotspots[compositeKey].Should().BeNull();

            eventCounter.Events.Should().HaveCount(1);
            eventCounter.Events[0].ProjectKey.Should().Be(projectKey);
            eventCounter.Events[0].BranchName.Should().Be(branchName);
        }

        [TestMethod]
        public async Task UpdateAsync_DifferentRecordFound_CallService()
        {
            var projectKey = nameof(UpdateAsync_RecordFound_CallsService);
            var branchName = "main";
            var compositeKey = ServerHotspotStore.CreateCompositeKey(projectKey, branchName);

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = new ServerHotspotStore(sonarQubeService.Object);
            var eventCounter = new EventCounter(testSubject);

            ServerHotspotStore.serverHotspots.Add("some other key", new List<SonarQubeHotspotSearch>());

            //Check if the test in correct state
            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeFalse();

            await testSubject.UpdateAsync(projectKey, branchName, CancellationToken.None);

            sonarQubeService.Verify(s => s.SearchHotspotsAsync(projectKey, branchName, It.IsAny<CancellationToken>()), Times.Once);
            sonarQubeService.VerifyNoOtherCalls();

            ServerHotspotStore.serverHotspots.ContainsKey(compositeKey).Should().BeTrue();

            eventCounter.Events.Should().HaveCount(1);
            eventCounter.Events[0].ProjectKey.Should().Be(projectKey);
            eventCounter.Events[0].BranchName.Should().Be(branchName);
        }

        [TestMethod]
        public async Task GetServerHotspotsAsync_RecordFound_DoNotCallService()
        {
            var projectKey = nameof(GetServerHotspotsAsync_RecordFound_DoNotCallService);
            var branchName = "main";
            var compositeKey = ServerHotspotStore.CreateCompositeKey(projectKey, branchName);

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = new ServerHotspotStore(sonarQubeService.Object);
            var eventCounter = new EventCounter(testSubject);

            var hotspotSearch1 = new SonarQubeHotspotSearch("key1", null, null, null, null, null, null, null);

            ServerHotspotStore.serverHotspots.Add(compositeKey, new[] { hotspotSearch1 });

            var result = await testSubject.GetServerHotspotsAsync(projectKey, branchName, CancellationToken.None);

            sonarQubeService.VerifyNoOtherCalls();

            eventCounter.Events.Should().BeEmpty();

            result.Should().ContainSingle();

            result[0].HotspotKey.Should().Be("key1");
        }

        [TestMethod]
        public async Task GetServerHotspotsAsync_RecordNotFound_CallService()
        {
            var projectKey = nameof(GetServerHotspotsAsync_RecordNotFound_CallService);
            var branchName = "main";
            var compositeKey = ServerHotspotStore.CreateCompositeKey(projectKey, branchName);

            var sonarQubeService = new Mock<ISonarQubeService>();

            var testSubject = new ServerHotspotStore(sonarQubeService.Object);
            var eventCounter = new EventCounter(testSubject);

            var hotspotSearch1 = new SonarQubeHotspotSearch("key1", null, null, null, null, null, null, null);

            sonarQubeService.Setup(s => s.SearchHotspotsAsync(projectKey, branchName, CancellationToken.None)).ReturnsAsync(new List<SonarQubeHotspotSearch> { hotspotSearch1 });

            var result = await testSubject.GetServerHotspotsAsync(projectKey, branchName, CancellationToken.None);

            sonarQubeService.Verify(s => s.SearchHotspotsAsync(projectKey, branchName, It.IsAny<CancellationToken>()), Times.Once);
            sonarQubeService.VerifyNoOtherCalls();

            eventCounter.Events.Should().HaveCount(1);
            eventCounter.Events[0].ProjectKey.Should().Be(projectKey);
            eventCounter.Events[0].BranchName.Should().Be(branchName);

            result.Should().ContainSingle();

            result[0].HotspotKey.Should().Be("key1");
        }

        private ServerHotspotStore CreateTestSubject(ISonarQubeService sonarQubeService)
        {
            return new ServerHotspotStore(sonarQubeService);
        }

        [TestCleanup]
        public void CleanUp()
        {
            ServerHotspotStore.serverHotspots.Clear();
        }

        private class EventCounter
        {
            private readonly List<ServerHotspotStoreUpdatedEventArgs> events = new List<ServerHotspotStoreUpdatedEventArgs>();

            public IReadOnlyList<ServerHotspotStoreUpdatedEventArgs> Events => events;

            public EventCounter(IServerHotspotStore store)
            {
                store.ServerHotspotStoreUpdated += (_, e) =>
                {
                    events.Add(e);
                };
            }
        }
    }
}
