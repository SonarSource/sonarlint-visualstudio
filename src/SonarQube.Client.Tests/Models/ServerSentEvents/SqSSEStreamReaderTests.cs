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

using System.IO;
using System.Text;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class SqSSEStreamReaderTests
    {
        [TestMethod, Timeout(10000)]
        [DataRow("")]
        [DataRow("some data\nanother data")]
        public async Task ReadAsync_EndOfStream_TaskFinishes(string content)
        {
            var networkStreamReader = CreateNetworkStreamReader(content);

            var testSubject = CreateTestSubject(networkStreamReader);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
            networkStreamReader.EndOfStream.Should().BeTrue();
        }

        [TestMethod, Timeout(10000)]
        public async Task ReadAsync_TokenIsCancelledBeforeStreamIsFinished_TaskFinishes()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "some data\nanother data\n");
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            var testSubject = CreateTestSubject(networkStreamReader, token: cancellationToken.Token);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
            networkStreamReader.EndOfStream.Should().BeFalse();
        }

        [TestMethod, Timeout(10000)]
        public async Task ReadAsync_StreamLinesAreAggregatedUntilAnEmptyLine()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "line 1\nline 2\nline 3\n\nline 4\nline 5\n\nline 6\nline 7\n");
            var parsedEvent1 = Mock.Of<ISqServerEvent>();
            var parsedEvent2 = Mock.Of<ISqServerEvent>();

            var sqServerSentEventParser = new Mock<ISqServerSentEventParser>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] {"line 1", "line 2", "line 3"}))
                .Returns(parsedEvent1);

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 4", "line 5" }))
                .Returns(parsedEvent2);

            var testSubject = CreateTestSubject(networkStreamReader, sqServerSentEventParser.Object);

            var actualEvent1 = await testSubject.ReadAsync();
            actualEvent1.Should().Be(parsedEvent1);
            networkStreamReader.EndOfStream.Should().BeFalse();

            var actualEvent2 = await testSubject.ReadAsync();
            actualEvent2.Should().Be(parsedEvent2);
            networkStreamReader.EndOfStream.Should().BeFalse();

            var actualEvent3 = await testSubject.ReadAsync();
            actualEvent3.Should().BeNull();
            networkStreamReader.EndOfStream.Should().BeTrue();

            sqServerSentEventParser.VerifyAll();
            sqServerSentEventParser.VerifyNoOtherCalls();
        }

        [TestMethod, Timeout(10000)]
        public async Task ReadAsync_FailureToParseAnEvent_EventIsIgnored()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "line 1\n\nline 2\nline 3\n\n");
            var sqServerSentEventParser = new Mock<ISqServerSentEventParser>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 1" }))
                .Returns((ISqServerEvent) null);

            var parsedEvent = Mock.Of<ISqServerEvent>();

            sqServerSentEventParser
                .Setup(x => x.Parse(new[] { "line 2", "line 3" }))
                .Returns(parsedEvent);

            var testSubject = CreateTestSubject(networkStreamReader, sqServerSentEventParser.Object);

            var actualEvent = await testSubject.ReadAsync();
            actualEvent.Should().Be(parsedEvent);
            networkStreamReader.EndOfStream.Should().BeTrue();

            sqServerSentEventParser.VerifyAll();
            sqServerSentEventParser.VerifyNoOtherCalls();
        }

        [TestMethod, Timeout(10000)]
        public async Task ReadAsync_StreamCrashesInTheMiddle_Exception()
        {
            var networkStreamReader = CreateNetworkStreamReader(content: "line 1\n\nline 2\nline 3\n\n");
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            var testSubject = CreateTestSubject(networkStreamReader, token: cancellationToken.Token);

            await testSubject.ReadAsync();
            networkStreamReader.Close();

            Func<Task<ISqServerEvent>> func = async () => await testSubject.ReadAsync();

            func.Should().ThrowExactly<ObjectDisposedException>().And.Message.Should().Be("Cannot read from a closed TextReader.");
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

        private static StreamReader CreateNetworkStreamReader(string content) =>
            new(new MemoryStream(Encoding.UTF8.GetBytes(content)));

        private static SqSSEStreamReader CreateTestSubject(
            StreamReader networkStreamReader, 
            ISqServerSentEventParser sqServerSentEventParser = null,
            CancellationToken? token = null)
        {
            token ??= CancellationToken.None;

            return new SqSSEStreamReader(networkStreamReader, token.Value, sqServerSentEventParser);
        }
    }
}
