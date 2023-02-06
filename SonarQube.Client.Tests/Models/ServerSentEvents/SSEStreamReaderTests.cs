/*
 * SonarQube Client
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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;
using Newtonsoft.Json;
using SonarQube.Client.Logging;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class SSEStreamReaderTests
    {
        [TestMethod]
        public async Task ReadAsync_TokenIsCancelled_NullReturned()
        {
            var cancellationToken = new CancellationToken(canceled: true);
            var channel = CreateChannelWithEvents(Mock.Of<ISqServerEvent>());

            var testSubject = CreateTestSubject(sqEventsChannel: channel, cancellationToken: cancellationToken);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
            channel.Reader.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task ReadAsync_Null_NullReturned()
        {
            var channel = CreateChannelWithEvents((ISqServerEvent) null);

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
        }

        [TestMethod]
        [Description("SQ stream events that we do not support yet. We need to ignore them.")]
        public async Task ReadAsync_UnrecognizedEventType_NullReturned()
        {
            var channel = CreateChannelWithEvents(new SqServerEvent("some type 111", "some data"));

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task ReadAsync_FailureToDeserializeTheEventData_ExceptionLoggedAndNullReturned()
        {
            var channel = CreateChannelWithEvents(new SqServerEvent("IssueChanged", "some invalid data"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sqEventsChannel: channel, logger);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();

            logger.DebugMessages.Should().Contain(x =>
                x.Contains(nameof(JsonReaderException)) &&
                x.Contains("IssueChanged") &&
                x.Contains("some invalid data"));
        }

        [TestMethod, Description("Missing mandatory 'branchName' field")]
        public async Task ReadAsync_IssueChangedEventType_MissingMandatoryFields_ExceptionLoggedAndNullReturned()
        {
            const string serializedIssueChangedEvent =
                "{\"projectKey\": \"projectKey1\",\"issues\": [{\"issueKey\": \"key1\"}],\"resolved\": \"true\"}";

            var channel = CreateChannelWithEvents(new SqServerEvent("IssueChanged", serializedIssueChangedEvent));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sqEventsChannel: channel, logger);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();

            logger.DebugMessages.Should().Contain(x =>
                x.Contains(nameof(ArgumentNullException)) &&
                x.Contains("branchName") &&
                x.Contains("IssueChanged") &&
                x.Contains("projectKey1"));
        }

        [TestMethod]
        public async Task ReadAsync_IssueChangedEventType_DeserializedEvent()
        {
            const string serializedIssueChangedEvent =
                "{\"projectKey\": \"projectKey1\",\"issues\": [{\"issueKey\": \"key1\",\"branchName\": \"master\"}],\"resolved\": \"true\"}";

            var channel = CreateChannelWithEvents(new SqServerEvent("IssueChanged", serializedIssueChangedEvent));

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            var result = await testSubject.ReadAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<IssueChangedServerEvent>();
            result.Should().BeEquivalentTo(
                new IssueChangedServerEvent(
                    projectKey: "projectKey1",
                    isResolved: true,
                    issues: new[] { new BranchAndIssueKey("key1", "master") }));
        }

        [TestMethod]
        public async Task ReadAsync_TaintVulnerabilityClosedEventType_DeserializedEvent()
        {
            const string serializedTaintVulnerabilityClosedEvent =
                "{\"projectKey\": \"projectKey1\",\"key\": \"taintKey\"}";

            var channel = CreateChannelWithEvents(new SqServerEvent("TaintVulnerabilityClosed", serializedTaintVulnerabilityClosedEvent));

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            var result = await testSubject.ReadAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<TaintVulnerabilityClosedServerEvent>();
            result.Should().BeEquivalentTo(
                new TaintVulnerabilityClosedServerEvent(
                    projectKey: "projectKey1",
                    key: "taintKey"));
        }

        [TestMethod]
        public async Task ReadAsync_TaintVulnerabilityRaisedEventType_DeserializedEvent()
        {
            const string serializedTaintVulnerabilityRaisedEvent =
                "{\"key\": \"taintKey\",\"projectKey\": \"projectKey1\",\"branch\": \"master\" }";

            var channel = CreateChannelWithEvents(new SqServerEvent("TaintVulnerabilityRaised", serializedTaintVulnerabilityRaisedEvent));

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            var result = await testSubject.ReadAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<TaintVulnerabilityRaisedServerEvent>();
            result.Should().BeEquivalentTo(
                new TaintVulnerabilityRaisedServerEvent(
                    projectKey: "projectKey1",
                    key: "taintKey",
                    branch: "master"));
        }

        private Channel<ISqServerEvent> CreateChannelWithEvents(params ISqServerEvent[] events)
        {
            var channel = Channel.CreateUnbounded<ISqServerEvent>();

            foreach (var sqServerEvent in events)
            {
                channel.Writer.TryWrite(sqServerEvent);
            }

            return channel;
        }

        private SSEStreamReader CreateTestSubject(Channel<ISqServerEvent> sqEventsChannel, 
            ILogger logger = null,
            CancellationToken? cancellationToken = null)
        {
            logger ??= Mock.Of<ILogger>();
            cancellationToken ??= CancellationToken.None;

            return new SSEStreamReader(sqEventsChannel, cancellationToken.Value, logger);
        }
    }
}
