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

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class SSEStreamWriterTests
    {
        [TestMethod, Timeout(10000)]
        [DataRow("")]
        [DataRow("some data\nanother data")]
        public async Task BeginListening_EndOfStream_TaskFinishes(string content)
        {
            var networkStreamReader = CreateNetworkStreamReader(content);
            var channel = CreateChannel();

            var testSubject = CreateTestSubject(networkStreamReader, channel);

            await testSubject.BeginListening();

            networkStreamReader.EndOfStream.Should().BeTrue();
            channel.Reader.Count.Should().Be(0);
        }

        [TestMethod, Timeout(10000)]
        public async Task BeginListening_TokenIsCancelledBeforeStreamIsFinished_TaskFinishes()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "some data\nanother data\n");
            var channel = CreateChannel();
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            var testSubject = CreateTestSubject(networkStreamReader, channel, token: cancellationToken.Token);

            await testSubject.BeginListening();

            networkStreamReader.EndOfStream.Should().BeFalse();
            channel.Reader.Count.Should().Be(0);
        }

        [TestMethod, Timeout(10000)]
        public async Task BeginListening_StreamLinesAreAggregatedUntilAnEmptyLine()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "line 1\nline 2\nline 3\n\nline 4\nline 5\n\nline 6\nline 7\n");
            var channel = CreateChannel();
            var parsedEvent1 = Mock.Of<ISqServerEvent>();
            var parsedEvent2 = Mock.Of<ISqServerEvent>();

            var sqServerSentEventParser = new Mock<ISqServerSentEventParser>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] {"line 1", "line 2", "line 3"}))
                .Returns(parsedEvent1);

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 4", "line 5" }))
                .Returns(parsedEvent2);

            var testSubject = CreateTestSubject(networkStreamReader, channel, sqServerSentEventParser.Object);

            await testSubject.BeginListening();

            networkStreamReader.EndOfStream.Should().BeTrue();
            channel.Reader.Count.Should().Be(2);

            channel.Reader.TryRead(out var actualEvent1).Should().BeTrue();
            actualEvent1.Should().Be(parsedEvent1);

            channel.Reader.TryRead(out var actualEvent2).Should().BeTrue();
            actualEvent2.Should().Be(parsedEvent2);

            sqServerSentEventParser.VerifyAll();
            sqServerSentEventParser.VerifyNoOtherCalls();
        }

        [TestMethod, Timeout(10000)]
        public async Task BeginListening_FailureToParseAnEvent_EventIsIgnored()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "line 1\n\nline 2\nline 3\n\n");
            var channel = CreateChannel();
            var sqServerSentEventParser = new Mock<ISqServerSentEventParser>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 1" }))
                .Returns((ISqServerEvent) null);

            var parsedEvent = Mock.Of<ISqServerEvent>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 2", "line 3" }))
                .Returns(parsedEvent);

            var testSubject = CreateTestSubject(networkStreamReader, channel, sqServerSentEventParser.Object);

            await testSubject.BeginListening();

            networkStreamReader.EndOfStream.Should().BeTrue();
            channel.Reader.Count.Should().Be(1);

            channel.Reader.TryRead(out var actualEvent).Should().BeTrue();
            actualEvent.Should().Be(parsedEvent);

            sqServerSentEventParser.VerifyAll();
            sqServerSentEventParser.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_ClosesStream()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "content");

            var testSubject = CreateTestSubject(networkStreamReader);

            networkStreamReader.BaseStream.Should().NotBeNull();

            testSubject.Dispose();

            networkStreamReader.BaseStream.Should().BeNull();
        }

        [TestMethod]
        public void Dispose_ClosesChannel()
        {
            var channel = CreateChannel();

            var testSubject = CreateTestSubject(sqEventsChannel: channel);

            channel.Writer.TryWrite(null).Should().BeTrue();

            testSubject.Dispose();

            channel.Writer.TryWrite(null).Should().BeFalse();
        }

        private static StreamReader CreateNetworkStreamReader(string content) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        private static Channel<ISqServerEvent> CreateChannel() => Channel.CreateUnbounded<ISqServerEvent>();

        private static SSEStreamWriter CreateTestSubject(
            StreamReader networkStreamReader = null, 
            ChannelWriter<ISqServerEvent> sqEventsChannel = null,
            ISqServerSentEventParser sqServerSentEventParser = null,
            CancellationToken? token = null)
        {
            token ??= CancellationToken.None;
            sqEventsChannel ??= CreateChannel();
            networkStreamReader ??= CreateNetworkStreamReader("");

            return new SSEStreamWriter(networkStreamReader, sqEventsChannel, token.Value, sqServerSentEventParser);
        }
    }
}
