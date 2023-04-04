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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Models;
using SonarQube.Client.Tests.Infra;
using static SonarQube.Client.Tests.Infra.MocksHelper;

namespace SonarQube.Client.Tests
{
    [TestClass]
    public class SonarQubeService_GetTaintVulnerabilitiesRequest : SonarQubeService_TestBase
    {
        [TestMethod]
        public void GetTaintVulnerabilitiesAsync_NotConnected()
        {
            // No calls to Connect
            // No need to setup request, the operation should fail
            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None);

            func.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("This operation expects the service to be connected.");

            logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
        }

        [TestMethod]
        public async Task GetTaintVulnerabilitiesAsync_Response_From_SonarQube()
        {
            await ConnectToSonarQube("8.6.0.0");

            SetupRequest("api/issues/search?projects=shared&statuses=OPEN%2CCONFIRMED%2CREOPENED&types=VULNERABILITY&p=1&ps=500", @"
{
	""total"": 4,
	""p"": 1,
	""ps"": 100,
	""paging"": {
		""pageIndex"": 1,
		""pageSize"": 100,
		""total"": 4
	},
	""effortTotal"": 72,
	""debtTotal"": 72,
	""issues"": [
		{
			""key"": ""AW0p2QsM-y65ELkujuR4"",
			""rule"": ""java:S4426"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 12,
			""hash"": ""e6162855f182ddc1652ec2cdea0a085b"",
			""textRange"": {
				""startLine"": 12,
				""endLine"": 12,
				""startOffset"": 6,
				""endOffset"": 32
			},
			""flows"": [],
			""status"": ""REOPENED"",
			""message"": ""Use a key length of at least 2048 bits."",
			""effort"": ""2min"",
			""debt"": ""2min"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a3""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-12T13:05:53+0000"",
			""updateDate"": ""2020-08-24T09:10:42+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
			""key"": ""AW0p2Qrv-y65ELkujuR0"",
			""rule"": ""java:S3330"",
			""severity"": ""CRITICAL"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 10,
			""hash"": ""79fd1a1af84dcccce2e0ede4970663ce"",
			""textRange"": {
				""startLine"": 10,
				""endLine"": 10,
				""startOffset"": 15,
				""endOffset"": 21
			},
			""flows"": [],
			""status"": ""OPEN"",
			""message"": ""Add the \""HttpOnly\"" cookie attribute."",
			""effort"": ""10min"",
			""debt"": ""10min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a7"",
				""sans-top25-insecure""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0000"",
			""updateDate"": ""2019-09-11T12:29:47+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
			""key"": ""AW0p2Qpn-y65ELkujuRf"",
			""rule"": ""javasecurity:S2076"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 17,
			""hash"": ""1703916771e6abb765843e62a76fcb5a"",
			""textRange"": {
				""startLine"": 17,
				""endLine"": 17,
				""startOffset"": 4,
				""endOffset"": 25
			},
			""flows"": [
				{
					""locations"": [
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 17,
								""endLine"": 17,
								""startOffset"": 4,
								""endOffset"": 25
							},
							""msg"": ""tainted value is used to perform a security-sensitive operation""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 14,
								""endLine"": 14,
								""startOffset"": 6,
								""endOffset"": 40
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 8,
								""endLine"": 8,
								""startOffset"": 9,
								""endOffset"": 38
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 38,
								""endLine"": 38,
								""startOffset"": 6,
								""endOffset"": 54
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 6,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 22,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 4,
								""endOffset"": 59
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 20,
								""endOffset"": 59
							},
							""msg"": ""this value can be controlled by the user""
						}
					]
				}
			],
			""status"": ""OPEN"",
			""message"": ""Refactor this code to not construct the OS command from tainted, user-controlled data."",
			""effort"": ""30min"",
			""debt"": ""30min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a1"",
				""sans-top25-insecure""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0000"",
			""updateDate"": ""2019-09-11T12:29:47+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
			""key"": ""AW0p2QqO-y65ELkujuRk"",
			""rule"": ""javasecurity:S3649"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 20,
			""hash"": ""b07a403eb4e77c8d6c4b3fa5c6408064"",
			""textRange"": {
				""startLine"": 20,
				""endLine"": 20,
				""startOffset"": 6,
				""endOffset"": 23
			},
			""flows"": [
				{
					""locations"": [
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
							""textRange"": {
								""startLine"": 20,
								""endLine"": 20,
								""startOffset"": 6,
								""endOffset"": 23
							},
							""msg"": ""tainted value is used to perform a security-sensitive operation""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
							""textRange"": {
								""startLine"": 9,
								""endLine"": 9,
								""startOffset"": 9,
								""endOffset"": 57
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 43,
								""endLine"": 43,
								""startOffset"": 6,
								""endOffset"": 68
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 43,
								""endLine"": 43,
								""startOffset"": 59,
								""endOffset"": 67
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 6,
								""endOffset"": 82
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 23,
								""endOffset"": 81
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 18,
								""endLine"": 18,
								""startOffset"": 8,
								""endOffset"": 43
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 18,
								""endLine"": 18,
								""startOffset"": 15,
								""endOffset"": 42
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 17,
								""endLine"": 17,
								""startOffset"": 39,
								""endOffset"": 55
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 14,
								""endLine"": 14,
								""startOffset"": 8,
								""endOffset"": 36
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 11,
								""endLine"": 11,
								""startOffset"": 27,
								""endOffset"": 50
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 23,
								""endOffset"": 81
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 6,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 22,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 4,
								""endOffset"": 59
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 20,
								""endOffset"": 59
							},
							""msg"": ""this value can be controlled by the user""
						}
					]
				}
			],
			""status"": ""OPEN"",
			""message"": ""Refactor this code to not construct SQL queries directly from tainted user-controlled data."",
			""effort"": ""30min"",
			""debt"": ""30min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cert"",
				""cwe"",
				""owasp-a1"",
				""sans-top25-insecure"",
				""sql""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0100"",
			""updateDate"": ""2019-09-16T10:35:19+1300"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		}
	],
	""components"": [
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRO"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""Servlet.java"",
			""longName"": ""src/main/java/foo/security/injection/Servlet.java"",
			""path"": ""src/main/java/foo/security/injection/Servlet.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""uuid"": ""AW05pEYnBvApAtP9iYg6"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""longName"": ""src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""path"": ""src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRT"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""WritingCookieServlet.java"",
			""longName"": ""src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""path"": ""src/main/java/foo/security/hotspots/WritingCookieServlet.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java"",
			""uuid"": ""AW0abn1qGHw5MqdAqloE"",
			""enabled"": true,
			""qualifier"": ""TRK"",
			""name"": ""City Tour - Java project"",
			""longName"": ""City Tour - Java project""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRX"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""KeyPairUtil.java"",
			""longName"": ""src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""path"": ""src/main/java/foo/security/crypto/KeyPairUtil.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRJ"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""BusinessThingsUtils.java"",
			""longName"": ""src/main/java/foo/security/injection/BusinessThingsUtils.java"",
			""path"": ""src/main/java/foo/security/injection/BusinessThingsUtils.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRL"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""CommandInjectionVulnerability.java"",
			""longName"": ""src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""path"": ""src/main/java/foo/security/injection/CommandInjectionVulnerability.java""
		}
	],
	""rules"": [
		{
			""key"": ""java:S3330"",
			""name"": ""Creating cookies without the \""HttpOnly\"" flag is security-sensitive"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""javasecurity:S3649"",
			""name"": ""Database queries should not be vulnerable to injection attacks"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""javasecurity:S2076"",
			""name"": ""OS commands should not be vulnerable to command injection attacks"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""java:S4426"",
			""name"": ""Cryptographic keys should be robust"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		}
	]
}
");

            var result = await service.GetTaintVulnerabilitiesAsync("shared", null, CancellationToken.None);

            messageHandler.VerifyAll();
            secondaryIssueHashUpdater.Verify(x => x.UpdateHashesAsync(result, service, It.IsAny<CancellationToken>()));

            // should return only 2 taint out of 4 vulnerabilities
            result.Should().HaveCount(2);

            var taint1 = result[0];

            taint1.IssueKey.Should().Be("AW0p2Qpn-y65ELkujuRf");
            taint1.RuleId.Should().Be("javasecurity:S2076");
            taint1.Message.Should().Be("Refactor this code to not construct the OS command from tainted, user-controlled data.");
            taint1.FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\CommandInjectionVulnerability.java");
            taint1.Hash.Should().Be("1703916771e6abb765843e62a76fcb5a");
            taint1.CreationTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:25:08+0000"));
            taint1.LastUpdateTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:29:47+0000"));
            taint1.Flows.Should().NotBeEmpty();
            taint1.Flows.Count.Should().Be(1);
            taint1.Flows[0].Locations.Count.Should().Be(8);
            taint1.Flows[0].Locations[0].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\CommandInjectionVulnerability.java");
            taint1.Flows[0].Locations[0].TextRange.Should().BeEquivalentTo(new IssueTextRange(17, 17, 4, 25));
            taint1.Flows[0].Locations[0].Message.Should().Be("tainted value is used to perform a security-sensitive operation");
            taint1.Flows[0].Locations[3].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\Servlet.java");
            taint1.Flows[0].Locations[3].TextRange.Should().BeEquivalentTo(new IssueTextRange(38, 38, 6, 54));
            taint1.Flows[0].Locations[3].Message.Should().Be("taint value is propagated");
            taint1.TextRange.Should().BeEquivalentTo(new IssueTextRange(17, 17, 4, 25));

            var taint2 = result[1];

            taint2.IssueKey.Should().Be("AW0p2QqO-y65ELkujuRk");
            taint2.RuleId.Should().Be("javasecurity:S3649");
            taint2.Message.Should().Be("Refactor this code to not construct SQL queries directly from tainted user-controlled data.");
            taint2.FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\SQLInjectionVulnerabilityCollectionMultipleFiles.java");
            taint2.Hash.Should().Be("b07a403eb4e77c8d6c4b3fa5c6408064");
            taint2.CreationTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:25:08+0100"));
            taint2.LastUpdateTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-16T10:35:19+1300"));
            taint2.Flows.Should().NotBeEmpty();
            taint2.Flows.Count.Should().Be(1);
            taint2.Flows[0].Locations.Count.Should().Be(16);
            taint2.Flows[0].Locations[7].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\BusinessThingsUtils.java");
            taint2.Flows[0].Locations[7].TextRange.Should().BeEquivalentTo(new IssueTextRange(18, 18, 15, 42));
            taint2.Flows[0].Locations[7].Message.Should().Be("taint value is propagated");
            taint2.Flows[0].Locations[15].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\Servlet.java");
            taint2.Flows[0].Locations[15].TextRange.Should().BeEquivalentTo(new IssueTextRange(34, 34, 20, 59));
            taint2.Flows[0].Locations[15].Message.Should().Be("this value can be controlled by the user");
            taint2.TextRange.Should().BeEquivalentTo(new IssueTextRange(20, 20, 6, 23));
        }

        [TestMethod]
        public async Task GetTaintVulnerabilitiesWithContextAsync_Response_From_SonarQube()
        {
            await ConnectToSonarQube("9.6.0.0");

            SetupRequest("api/issues/search?additionalFields=ruleDescriptionContextKey&projects=shared&statuses=OPEN%2CCONFIRMED%2CREOPENED&types=VULNERABILITY&p=1&ps=500", @"
{
	""total"": 4,
	""p"": 1,
	""ps"": 100,
	""paging"": {
		""pageIndex"": 1,
		""pageSize"": 100,
		""total"": 4
	},
	""effortTotal"": 72,
	""debtTotal"": 72,
	""issues"": [
		{
			""key"": ""AW0p2QsM-y65ELkujuR4"",
			""rule"": ""java:S4426"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 12,
			""hash"": ""e6162855f182ddc1652ec2cdea0a085b"",
			""textRange"": {
				""startLine"": 12,
				""endLine"": 12,
				""startOffset"": 6,
				""endOffset"": 32
			},
			""flows"": [],
			""status"": ""REOPENED"",
			""message"": ""Use a key length of at least 2048 bits."",
			""effort"": ""2min"",
			""debt"": ""2min"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a3""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-12T13:05:53+0000"",
			""updateDate"": ""2020-08-24T09:10:42+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
			""key"": ""AW0p2Qrv-y65ELkujuR0"",
			""rule"": ""java:S3330"",
			""severity"": ""CRITICAL"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 10,
			""hash"": ""79fd1a1af84dcccce2e0ede4970663ce"",
			""textRange"": {
				""startLine"": 10,
				""endLine"": 10,
				""startOffset"": 15,
				""endOffset"": 21
			},
			""flows"": [],
			""status"": ""OPEN"",
			""message"": ""Add the \""HttpOnly\"" cookie attribute."",
			""effort"": ""10min"",
			""debt"": ""10min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a7"",
				""sans-top25-insecure""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0000"",
			""updateDate"": ""2019-09-11T12:29:47+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
            ""ruleDescriptionContextKey"": ""context"",
			""key"": ""AW0p2Qpn-y65ELkujuRf"",
			""rule"": ""javasecurity:S2076"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 17,
			""hash"": ""1703916771e6abb765843e62a76fcb5a"",
			""textRange"": {
				""startLine"": 17,
				""endLine"": 17,
				""startOffset"": 4,
				""endOffset"": 25
			},
			""flows"": [
				{
					""locations"": [
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 17,
								""endLine"": 17,
								""startOffset"": 4,
								""endOffset"": 25
							},
							""msg"": ""tainted value is used to perform a security-sensitive operation""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 14,
								""endLine"": 14,
								""startOffset"": 6,
								""endOffset"": 40
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
							""textRange"": {
								""startLine"": 8,
								""endLine"": 8,
								""startOffset"": 9,
								""endOffset"": 38
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 38,
								""endLine"": 38,
								""startOffset"": 6,
								""endOffset"": 54
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 6,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 22,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 4,
								""endOffset"": 59
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 20,
								""endOffset"": 59
							},
							""msg"": ""this value can be controlled by the user""
						}
					]
				}
			],
			""status"": ""OPEN"",
			""message"": ""Refactor this code to not construct the OS command from tainted, user-controlled data."",
			""effort"": ""30min"",
			""debt"": ""30min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cwe"",
				""owasp-a1"",
				""sans-top25-insecure""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0000"",
			""updateDate"": ""2019-09-11T12:29:47+0000"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		},
		{
			""key"": ""AW0p2QqO-y65ELkujuRk"",
			""rule"": ""javasecurity:S3649"",
			""severity"": ""BLOCKER"",
			""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""project"": ""com.sonarsource:citytour2019-java"",
			""line"": 20,
			""hash"": ""b07a403eb4e77c8d6c4b3fa5c6408064"",
			""textRange"": {
				""startLine"": 20,
				""endLine"": 20,
				""startOffset"": 6,
				""endOffset"": 23
			},
			""flows"": [
				{
					""locations"": [
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
							""textRange"": {
								""startLine"": 20,
								""endLine"": 20,
								""startOffset"": 6,
								""endOffset"": 23
							},
							""msg"": ""tainted value is used to perform a security-sensitive operation""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
							""textRange"": {
								""startLine"": 9,
								""endLine"": 9,
								""startOffset"": 9,
								""endOffset"": 57
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 43,
								""endLine"": 43,
								""startOffset"": 6,
								""endOffset"": 68
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 43,
								""endLine"": 43,
								""startOffset"": 59,
								""endOffset"": 67
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 6,
								""endOffset"": 82
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 23,
								""endOffset"": 81
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 18,
								""endLine"": 18,
								""startOffset"": 8,
								""endOffset"": 43
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 18,
								""endLine"": 18,
								""startOffset"": 15,
								""endOffset"": 42
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 17,
								""endLine"": 17,
								""startOffset"": 39,
								""endOffset"": 55
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 14,
								""endLine"": 14,
								""startOffset"": 8,
								""endOffset"": 36
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
							""textRange"": {
								""startLine"": 11,
								""endLine"": 11,
								""startOffset"": 27,
								""endOffset"": 50
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 41,
								""endLine"": 41,
								""startOffset"": 23,
								""endOffset"": 81
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 6,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 36,
								""endLine"": 36,
								""startOffset"": 22,
								""endOffset"": 72
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 4,
								""endOffset"": 59
							},
							""msg"": ""taint value is propagated""
						},
						{
							""component"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
							""textRange"": {
								""startLine"": 34,
								""endLine"": 34,
								""startOffset"": 20,
								""endOffset"": 59
							},
							""msg"": ""this value can be controlled by the user""
						}
					]
				}
			],
			""status"": ""OPEN"",
			""message"": ""Refactor this code to not construct SQL queries directly from tainted user-controlled data."",
			""effort"": ""30min"",
			""debt"": ""30min"",
			""assignee"": ""agigleux@github"",
			""author"": ""alexandre.gigleux@sonarsource.com"",
			""tags"": [
				""cert"",
				""cwe"",
				""owasp-a1"",
				""sans-top25-insecure"",
				""sql""
			],
			""transitions"": [
				""confirm"",
				""resolve"",
				""falsepositive"",
				""wontfix""
			],
			""actions"": [
				""set_type"",
				""set_tags"",
				""comment"",
				""set_severity"",
				""assign""
			],
			""comments"": [],
			""creationDate"": ""2019-09-11T12:25:08+0100"",
			""updateDate"": ""2019-09-16T10:35:19+1300"",
			""type"": ""VULNERABILITY"",
			""organization"": ""default-organization"",
			""scope"": ""MAIN""
		}
	],
	""components"": [
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/Servlet.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRO"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""Servlet.java"",
			""longName"": ""src/main/java/foo/security/injection/Servlet.java"",
			""path"": ""src/main/java/foo/security/injection/Servlet.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""uuid"": ""AW05pEYnBvApAtP9iYg6"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""longName"": ""src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java"",
			""path"": ""src/main/java/foo/security/injection/SQLInjectionVulnerabilityCollectionMultipleFiles.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRT"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""WritingCookieServlet.java"",
			""longName"": ""src/main/java/foo/security/hotspots/WritingCookieServlet.java"",
			""path"": ""src/main/java/foo/security/hotspots/WritingCookieServlet.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java"",
			""uuid"": ""AW0abn1qGHw5MqdAqloE"",
			""enabled"": true,
			""qualifier"": ""TRK"",
			""name"": ""City Tour - Java project"",
			""longName"": ""City Tour - Java project""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRX"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""KeyPairUtil.java"",
			""longName"": ""src/main/java/foo/security/crypto/KeyPairUtil.java"",
			""path"": ""src/main/java/foo/security/crypto/KeyPairUtil.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/BusinessThingsUtils.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRJ"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""BusinessThingsUtils.java"",
			""longName"": ""src/main/java/foo/security/injection/BusinessThingsUtils.java"",
			""path"": ""src/main/java/foo/security/injection/BusinessThingsUtils.java""
		},
		{
			""organization"": ""default-organization"",
			""key"": ""com.sonarsource:citytour2019-java:src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""uuid"": ""AW0p2QjJ-y65ELkujuRL"",
			""enabled"": true,
			""qualifier"": ""FIL"",
			""name"": ""CommandInjectionVulnerability.java"",
			""longName"": ""src/main/java/foo/security/injection/CommandInjectionVulnerability.java"",
			""path"": ""src/main/java/foo/security/injection/CommandInjectionVulnerability.java""
		}
	],
	""rules"": [
		{
			""key"": ""java:S3330"",
			""name"": ""Creating cookies without the \""HttpOnly\"" flag is security-sensitive"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""javasecurity:S3649"",
			""name"": ""Database queries should not be vulnerable to injection attacks"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""javasecurity:S2076"",
			""name"": ""OS commands should not be vulnerable to command injection attacks"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		},
		{
			""key"": ""java:S4426"",
			""name"": ""Cryptographic keys should be robust"",
			""lang"": ""java"",
			""status"": ""READY"",
			""langName"": ""Java""
		}
	]
}
");

            var result = await service.GetTaintVulnerabilitiesAsync("shared", null, CancellationToken.None);

            messageHandler.VerifyAll();
            secondaryIssueHashUpdater.Verify(x => x.UpdateHashesAsync(result, service, It.IsAny<CancellationToken>()));

            // should return only 2 taint out of 4 vulnerabilities
            result.Should().HaveCount(2);

            var taint1 = result[0];

            taint1.IssueKey.Should().Be("AW0p2Qpn-y65ELkujuRf");
            taint1.RuleId.Should().Be("javasecurity:S2076");
            taint1.Message.Should().Be("Refactor this code to not construct the OS command from tainted, user-controlled data.");
            taint1.FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\CommandInjectionVulnerability.java");
            taint1.Hash.Should().Be("1703916771e6abb765843e62a76fcb5a");
            taint1.CreationTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:25:08+0000"));
            taint1.LastUpdateTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:29:47+0000"));
            taint1.Flows.Should().NotBeEmpty();
            taint1.Flows.Count.Should().Be(1);
            taint1.Flows[0].Locations.Count.Should().Be(8);
            taint1.Flows[0].Locations[0].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\CommandInjectionVulnerability.java");
            taint1.Flows[0].Locations[0].TextRange.Should().BeEquivalentTo(new IssueTextRange(17, 17, 4, 25));
            taint1.Flows[0].Locations[0].Message.Should().Be("tainted value is used to perform a security-sensitive operation");
            taint1.Flows[0].Locations[3].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\Servlet.java");
            taint1.Flows[0].Locations[3].TextRange.Should().BeEquivalentTo(new IssueTextRange(38, 38, 6, 54));
            taint1.Flows[0].Locations[3].Message.Should().Be("taint value is propagated");
            taint1.TextRange.Should().BeEquivalentTo(new IssueTextRange(17, 17, 4, 25));
            taint1.Context.Should().Be("context");

            var taint2 = result[1];

            taint2.IssueKey.Should().Be("AW0p2QqO-y65ELkujuRk");
            taint2.RuleId.Should().Be("javasecurity:S3649");
            taint2.Message.Should().Be("Refactor this code to not construct SQL queries directly from tainted user-controlled data.");
            taint2.FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\SQLInjectionVulnerabilityCollectionMultipleFiles.java");
            taint2.Hash.Should().Be("b07a403eb4e77c8d6c4b3fa5c6408064");
            taint2.CreationTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-11T12:25:08+0100"));
            taint2.LastUpdateTimestamp.Should().Be(DateTimeOffset.Parse("2019-09-16T10:35:19+1300"));
            taint2.Flows.Should().NotBeEmpty();
            taint2.Flows.Count.Should().Be(1);
            taint2.Flows[0].Locations.Count.Should().Be(16);
            taint2.Flows[0].Locations[7].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\BusinessThingsUtils.java");
            taint2.Flows[0].Locations[7].TextRange.Should().BeEquivalentTo(new IssueTextRange(18, 18, 15, 42));
            taint2.Flows[0].Locations[7].Message.Should().Be("taint value is propagated");
            taint2.Flows[0].Locations[15].FilePath.Should().Be("src\\main\\java\\foo\\security\\injection\\Servlet.java");
            taint2.Flows[0].Locations[15].TextRange.Should().BeEquivalentTo(new IssueTextRange(34, 34, 20, 59));
            taint2.Flows[0].Locations[15].Message.Should().Be("this value can be controlled by the user");
            taint2.TextRange.Should().BeEquivalentTo(new IssueTextRange(20, 20, 6, 23));
            taint2.Context.Should().BeNull();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public async Task GetTaintVulnerabilitiesAsync_BranchIsNotSpecified_BranchIsNotIncludedInQueryString(string emptyBranch)
        {
            await ConnectToSonarQube("8.6.0.0");
            messageHandler.Reset();

            SetupHttpRequest(messageHandler, EmptyGetIssuesResponse);
            _ = await service.GetTaintVulnerabilitiesAsync("any", emptyBranch, CancellationToken.None);

            // Branch is null/empty => should not be passed
            var actualRequests = messageHandler.GetSendAsyncRequests();
            actualRequests.Should().ContainSingle();
            actualRequests[0].RequestUri.Query.Contains("branch").Should().BeFalse();
        }

        [TestMethod]
        public async Task GetTaintVulnerabilitiesAsync_BranchIsSpecified_BranchIncludedInQueryString()
        {
            await ConnectToSonarQube("8.6.0.0");
            messageHandler.Reset();

            SetupHttpRequest(messageHandler, EmptyGetIssuesResponse);
            _ = await service.GetTaintVulnerabilitiesAsync("any", "aBranch", CancellationToken.None);

            var actualRequests = messageHandler.GetSendAsyncRequests();
            actualRequests.Should().ContainSingle();
            actualRequests[0].RequestUri.Query.Contains("&branch=aBranch&").Should().BeTrue();
        }
    }
}
