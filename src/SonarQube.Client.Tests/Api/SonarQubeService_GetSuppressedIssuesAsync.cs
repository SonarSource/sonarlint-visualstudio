/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_GetSuppressedIssuesAsync : SonarQubeService_TestBase
    {
        [TestMethod]
        public async Task GetSuppressedIssuesAsync_Old_ExampleFromSonarQube()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1",
                new StreamReader(@"TestResources\IssuesProtobufResponse").ReadToEnd());

            var result = await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            // TODO: create a protobuf file with more than one issue with different states
            // the one above does not have suppressed issues, hence the Count==0
            result.Should().HaveCount(0);

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_Old_NotFound()
        {
            await ConnectToSonarQube();

            SetupRequest("batch/issues?key=project1", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupRequest("api/issues/search?projects=simplcom&p=1&ps=500", @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 1
  },
  ""issues"": [{
    ""key"": ""AWQYvw-4pbnviuOCCX9e"",
    ""rule"": ""csharpsquid:S1075"",
    ""severity"": ""MINOR"",
    ""component"": ""simplcom:simplcom:91A9DD1C-08F3-4F85-8728-BB30F55FDACB:Controllers/ThemeApiController.cs"",
    ""project"": ""simplcom"",
    ""subProject"": ""simplcom:simplcom:91A9DD1C-08F3-4F85-8728-BB30F55FDACB"",
    ""line"": 36,
    ""hash"": ""0dcbf3b077bacc9fdbd898ff3b587085"",
    ""textRange"": {
        ""startLine"": 36,
        ""endLine"": 36,
        ""startOffset"": 49,
        ""endOffset"": 107
    },
    ""flows"": [],
    ""status"": ""OPEN"",
    ""message"": ""Refactor your code not to use hardcoded absolute paths or URIs."",
    ""effort"": ""20min"",
    ""debt"": ""20min"",
    ""author"": ""nlqthien@gmail.com"",
    ""tags"": [
        ""cert""
    ],
    ""creationDate"": ""2018-05-09T21:45:14+0200"",
    ""updateDate"": ""2018-06-27T12:02:40+0200"",
    ""type"": ""CODE_SMELL"",
    ""organization"": ""default-organization""
  },
  {
    ""key"": ""AWQYvwc4pbnviuOCCX4g"",
    ""rule"": ""csharpsquid:S1116"",
    ""severity"": ""MINOR"",
    ""component"": ""simplcom:simplcom:13367B7A-E91C-47EE-BA5E-C50664D65767:Extensions/ServiceCollectionExtensions.cs"",
    ""project"": ""simplcom"",
    ""subProject"": ""simplcom:simplcom:13367B7A-E91C-47EE-BA5E-C50664D65767"",
    ""line"": 136,
    ""hash"": ""065c8f8dd412a96eb30c22dfdf68b63f"",
    ""textRange"": {
        ""startLine"": 136,
        ""endLine"": 136,
        ""startOffset"": 76,
        ""endOffset"": 77
    },
    ""flows"": [],
    ""resolution"": ""WONTFIX"",
    ""status"": ""RESOLVED"",
    ""message"": ""Remove this empty statement."",
    ""effort"": ""2min"",
    ""debt"": ""2min"",
    ""author"": ""nlqthien@gmail.com"",
    ""tags"": [
        ""cert"",
        ""misra"",
        ""unused""
    ],
    ""creationDate"": ""2018-05-08T19:06:01+0200"",
    ""updateDate"": ""2018-06-27T12:02:40+0200"",
    ""type"": ""CODE_SMELL"",
    ""organization"": ""default-organization""
  },
  {
    ""key"": ""AWQYvwYapbnviuOCCX4D"",
    ""rule"": ""vbnet:S1186"",
    ""severity"": ""CRITICAL"",
    ""component"": ""simplcom:simplcom:B8ABFD73-AEAF-4689-BA2C-BC38B64F6FAE:ModuleInitializer.vb"",
    ""project"": ""simplcom"",
    ""subProject"": ""simplcom:simplcom:B8ABFD73-AEAF-4689-BA2C-BC38B64F6FAE"",
    ""line"": 17,
    ""hash"": ""c1c4472d365c673ec78eee5a2ef90831"",
    ""textRange"": {
        ""startLine"": 17,
        ""endLine"": 17,
        ""startOffset"": 20,
        ""endOffset"": 29
    },
    ""flows"": [],
    ""resolution"": ""FALSE-POSITIVE"",
    ""status"": ""RESOLVED"",
    ""message"": ""Add a nested comment explaining why this method is empty, throw a 'NotSupportedException' or complete the implementation."",
    ""effort"": ""5min"",
    ""debt"": ""5min"",
    ""author"": ""nlqthien@gmail.com"",
    ""tags"": [
        ""suspicious""
    ],
    ""creationDate"": ""2018-05-08T19:06:01+0200"",
    ""updateDate"": ""2018-06-27T12:02:40+0200"",
    ""type"": ""CODE_SMELL"",
    ""organization"": ""default-organization""
  }]
}
");

            var result = await service.GetSuppressedIssuesAsync("simplcom", CancellationToken.None);

            result.Should().HaveCount(2);

            var csharpIssue = result[0];
            csharpIssue.FilePath.Should().Be("Extensions/ServiceCollectionExtensions.cs");
            csharpIssue.Hash.Should().Be("065c8f8dd412a96eb30c22dfdf68b63f");
            csharpIssue.Line.Should().Be(136);
            csharpIssue.Message.Should().Be("Remove this empty statement.");
            csharpIssue.ModuleKey.Should().Be("simplcom:simplcom:13367B7A-E91C-47EE-BA5E-C50664D65767");
            csharpIssue.ResolutionState.Should().Be(SonarQubeIssueResolutionState.WontFix);
            csharpIssue.RuleId.Should().Be("S1116");

            var vbnetIssue = result[1];
            vbnetIssue.FilePath.Should().Be("ModuleInitializer.vb");
            vbnetIssue.Hash.Should().Be("c1c4472d365c673ec78eee5a2ef90831");
            vbnetIssue.Line.Should().Be(17);
            vbnetIssue.Message.Should().Be("Add a nested comment explaining why this method is empty, throw a 'NotSupportedException' or complete the implementation.");
            vbnetIssue.ModuleKey.Should().Be("simplcom:simplcom:B8ABFD73-AEAF-4689-BA2C-BC38B64F6FAE");
            vbnetIssue.ResolutionState.Should().Be(SonarQubeIssueResolutionState.FalsePositive);
            vbnetIssue.RuleId.Should().Be("S1186");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20_ModuleLevelIssue()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupRequest("api/issues/search?projects=simplcom&p=1&ps=500", @"
{
  ""paging"": {
    ""pageIndex"": 1,
    ""pageSize"": 100,
    ""total"": 1
  },
  ""issues"": [{
    ""key"": ""AWQYvwc4pbnviuOCCX4g"",
    ""rule"": ""csharpsquid:S1116"",
    ""severity"": ""MINOR"",
    ""component"": ""simplcom:simplcom:13367B7A-E91C-47EE-BA5E-C50664D65767"",
    ""project"": ""simplcom"",
    ""subProject"": null,
    ""line"": 136,
    ""hash"": ""065c8f8dd412a96eb30c22dfdf68b63f"",
    ""textRange"": {
        ""startLine"": 136,
        ""endLine"": 136,
        ""startOffset"": 76,
        ""endOffset"": 77
    },
    ""flows"": [],
    ""resolution"": ""WONTFIX"",
    ""status"": ""RESOLVED"",
    ""message"": ""Remove this empty statement."",
    ""effort"": ""2min"",
    ""debt"": ""2min"",
    ""author"": ""nlqthien@gmail.com"",
    ""tags"": [
        ""cert"",
        ""misra"",
        ""unused""
    ],
    ""creationDate"": ""2018-05-08T19:06:01+0200"",
    ""updateDate"": ""2018-06-27T12:02:40+0200"",
    ""type"": ""CODE_SMELL"",
    ""organization"": ""default-organization""
  }]
}
");

            var result = await service.GetSuppressedIssuesAsync("simplcom", CancellationToken.None);

            result.Should().HaveCount(1);

            var csharpIssue = result[0];
            csharpIssue.FilePath.Should().Be(string.Empty); // Module level issue
            csharpIssue.Hash.Should().Be("065c8f8dd412a96eb30c22dfdf68b63f");
            csharpIssue.Line.Should().Be(136);
            csharpIssue.Message.Should().Be("Remove this empty statement.");
            csharpIssue.ModuleKey.Should().Be("simplcom:simplcom:13367B7A-E91C-47EE-BA5E-C50664D65767");
            csharpIssue.ResolutionState.Should().Be(SonarQubeIssueResolutionState.WontFix);
            csharpIssue.RuleId.Should().Be("S1116");
                        
            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20_NotFound()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupRequest("api/issues/search?projects=project1&p=1&ps=500", "", HttpStatusCode.NotFound);

            Func<Task<IList<SonarQubeIssue>>> func = async () =>
                await service.GetSuppressedIssuesAsync("project1", CancellationToken.None);

            func.Should().ThrowExactly<HttpRequestException>().And
                .Message.Should().Be("Response status code does not indicate success: 404 (Not Found).");

            messageHandler.VerifyAll();
        }

        [TestMethod]
        public async Task GetSuppressedIssuesAsync_From_7_20_Paging()
        {
            await ConnectToSonarQube("7.2.0.0");

            SetupRequest("api/issues/search?projects=simplcom&p=1&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 1,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""issues"": [
    {string.Join(",\n", Enumerable.Range(1, 500).Select(CreateIssueJson))}
  ]
}}");

            SetupRequest("api/issues/search?projects=simplcom&p=2&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 2,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""issues"": [
    {string.Join(",\n", Enumerable.Range(501, 500).Select(CreateIssueJson))}
  ]
}}");

            SetupRequest("api/issues/search?projects=simplcom&p=3&ps=500", $@"
{{
  ""paging"": {{
    ""pageIndex"": 3,
    ""pageSize"": 500,
    ""total"": 3
  }},
  ""issues"": [
    {string.Join(",\n", Enumerable.Range(1001, 1).Select(CreateIssueJson))}
  ]
}}");


            var result = await service.GetSuppressedIssuesAsync("simplcom", CancellationToken.None);

            result.Should().HaveCount(1001);

            messageHandler.VerifyAll();
        }

        private static string CreateIssueJson(int number) =>
            "{ " +
            $"\"key\": \"{number}\", " +
            "\"rule\": \"csharpsquid:S1075\", " +
            "\"component\": \"simplcom:simplcom:91A9DD1C-08F3-4F85-8728-BB30F55FDACB:Controllers/ThemeApiController.cs\", " +
            "\"project\": \"simplcom\", " +
            "\"subProject\": \"simplcom:simplcom:91A9DD1C-08F3-4F85-8728-BB30F55FDACB\", " +
            "\"line\": 36, " +
            "\"hash\": \"0dcbf3b077bacc9fdbd898ff3b587085\", " +
            "\"resolution\": \"WONTFIX\", " +
            "\"message\": \"Refactor your code not to use hardcoded absolute paths or URIs.\" " +
            "}";
    }
}
