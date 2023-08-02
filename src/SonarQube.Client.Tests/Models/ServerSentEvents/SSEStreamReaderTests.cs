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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarQube.Client.Logging;
using SonarQube.Client.Models;
using SonarQube.Client.Models.ServerSentEvents;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;
using SonarQube.Client.Models.ServerSentEvents.ServerContract;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class SSEStreamReaderTests
    {
        [TestMethod]
        public void ReadAsync_UnderlyingReaderException_LogsDisposesAndThrows()
        {
            var streamReader = new Mock<ISqSSEStreamReader>();
            var exceptionMessage = "Some network error";
            streamReader.Setup(reader => reader.ReadAsync()).Throws(new Exception(exceptionMessage));

            var testSubject = CreateTestSubject(streamReader.Object);

            Func<Task> act = () => testSubject.ReadAsync();

            act.Should().Throw<Exception>().Which.Message.Should().Be(exceptionMessage);
            streamReader.Verify(reader => reader.Dispose(), Times.Once);
        }

        [TestMethod]
        public async Task ReadAsync_Null_NullReturned()
        {
            var sqSSEStreamReader = CreateSqStreamReader((ISqServerEvent)null);

            var testSubject = CreateTestSubject(sqSSEStreamReader);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
        }

        [TestMethod]
        [Description("SQ stream events that we do not support yet. We need to ignore them.")]
        public async Task ReadAsync_UnrecognizedEventType_NullReturned()
        {
            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("some type 111", "some data"));

            var testSubject = CreateTestSubject(sqSSEStreamReader);

            var result = await testSubject.ReadAsync();

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task ReadAsync_FailureToDeserializeTheEventData_ExceptionLoggedAndNullReturned()
        {
            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("IssueChanged", "some invalid data"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sqSSEStreamReader, logger);

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

            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("IssueChanged", serializedIssueChangedEvent));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(sqSSEStreamReader, logger);

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

            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("IssueChanged", serializedIssueChangedEvent));

            var testSubject = CreateTestSubject(sqSSEStreamReader);

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

            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("TaintVulnerabilityClosed", serializedTaintVulnerabilityClosedEvent));

            var testSubject = CreateTestSubject(sqSSEStreamReader);

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
            const string serializedTaintVulnerabilityRaisedEvent = @"{
	""key"": ""taintKey"",
	""projectKey"": ""projectKey1"",
    ""creationDate"": 1676390804000,
	""branch"": ""master"",
	""ruleKey"": ""javasecurity:S123"",
	""severity"": ""MAJOR"",
	""type"": ""VULNERABILITY"",
	""mainLocation"": {
		""filePath"": ""functions/taint.js"",
		""message"": ""blah blah"",
		""textRange"": {
			""startLine"": 17,
			""startLineOffset"": 10,
			""endLine"": 3,
			""endLineOffset"": 2,
			""hash"": ""hash""
		}
	},
	""flows"": [
		{
			""locations"": [
				{
					""filePath"": ""functions/taint.js"",
					""message"": ""sink""
				}
			]
		}
	],
    ""ruleDescriptionContextKey"": ""ContextKey""
}";
            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("TaintVulnerabilityRaised", serializedTaintVulnerabilityRaisedEvent));

            var testSubject = CreateTestSubject(sqSSEStreamReader);

            var result = await testSubject.ReadAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<TaintVulnerabilityRaisedServerEvent>();
            result.Should().BeEquivalentTo(
                new TaintVulnerabilityRaisedServerEvent(
                    projectKey: "projectKey1",
                    key: "taintKey",
                    creationDate: DateTimeOffset.Parse("2023-02-14T16:06:44+00:00"),
                    branch: "master",
                    ruleKey: "javasecurity:S123",
                    severity: SonarQubeIssueSeverity.Major,
                    type: SonarQubeIssueType.Vulnerability,
                    mainLocation:
                    new Location(
                        filePath: "functions/taint.js",
                        message: "blah blah",
                        textRange: new TextRange(17, 10, 3, 2, "hash")),
                    flows: new[]
                    {
                        new Flow(new[]
                        {
                            new Location(filePath: "functions/taint.js", message: "sink", textRange: null)
                        })
                    },
                    ruleDescriptionContextKey: "ContextKey"));
        }
        
        [TestMethod]
        public async Task ReadAsync_QualityProfileEventType_DeserializedEvent()
        {
            const string serializedQualityProfileEvent = """
                {
                  "projects": [
                    "ABC"
                  ],
                  "activatedRules": [
                    {
                      "key": "javascript:S4139",
                      "language": "js",
                      "severity": "MAJOR",
                      "params": []
                    }
                  ],
                  "deactivatedRules": []
                }
                """;
            
            var sqSSEStreamReader = CreateSqStreamReader(new SqServerEvent("RuleSetChanged", serializedQualityProfileEvent));

            var testSubject = CreateTestSubject(sqSSEStreamReader);

            var result = await testSubject.ReadAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<QualityProfileEvent>();
            // note: event implementation is empty, no test for data validity here either
        }

        private ISqSSEStreamReader CreateSqStreamReader(params ISqServerEvent[] events)
        {
            var streamReader = new Mock<ISqSSEStreamReader>();
            var sequenceSetup = streamReader.SetupSequence(x => x.ReadAsync());

            foreach (var sqServerEvent in events)
            {
                sequenceSetup.ReturnsAsync(sqServerEvent);
            }

            return streamReader.Object;
        }

        private SSEStreamReader CreateTestSubject(ISqSSEStreamReader sqSSEStreamReader, ILogger logger = null)
        {
            logger ??= Mock.Of<ILogger>();

            return new SSEStreamReader(sqSSEStreamReader, logger);
        }
    }
}
