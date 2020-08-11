/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.IntegrationTests
{
    [TestClass]
    public class CFamily_CLangAnalyzer_IntegrationTests
    {
        private string testsDataDirectory;

        [TestInitialize]
        public void TestInitialize()
        {
            testsDataDirectory = Path.Combine(
                Path.GetDirectoryName(typeof(CFamily_CLangAnalyzer_IntegrationTests).Assembly.Location),
                "CFamily\\IntegrationTests\\");
        }

        [TestMethod]
        [DataRow("CLangAnalyzerTestFile_NoIssues_EmptyFile")]
        [DataRow("CLangAnalyzerTestFile_OneIssue")]
        [DataRow("CLangAnalyzerTestFile_TwoIssues")]
        [DataRow("CLangAnalyzerTestFile_OneIssue_HasSecondaryLocations")]
        public void CallAnalyzer_IntegrationTest(string testCaseFileName)
        {
            var testedFile = Path.Combine(testsDataDirectory, testCaseFileName + ".txt");

            var request = GetRequest(testedFile);
            var expectedMessages = GetExpectedMessages(testCaseFileName, testedFile);

            var messages = InvokeAnalyzer(request);

            messages.Should().BeEquivalentTo(expectedMessages, e => e.WithStrictOrdering());
        }

        private Request GetRequest(string testedFile)
        {
            var requestJson = File.ReadAllText(Path.Combine(testsDataDirectory, "CLangAnalyzerRequestTemplate.json"));
            var request = JsonConvert.DeserializeObject<Request>(requestJson);
            request.File = testedFile;

            return request;
        }

        private Message[] GetExpectedMessages(string testFileName, string testedFile)
        {
            var expectedResponseJson = File.ReadAllText(Path.Combine(testsDataDirectory, testFileName + "_response.json"));
            var expectedResponse = JsonConvert.DeserializeObject<Response>(expectedResponseJson);

            foreach (var expectedResponseMessage in expectedResponse.Messages)
            {
                expectedResponseMessage.Filename = testedFile;

                foreach (var messagePart in expectedResponseMessage.Parts)
                {
                    messagePart.Filename = testedFile;
                }
            }

            return expectedResponse.Messages;
        }

        private static List<Message> InvokeAnalyzer(Request request)
        {
            var testLogger = new TestLogger(true);
            var processRunner = new ProcessRunner(new ConfigurableSonarLintSettings(), testLogger);

            var messages = new List<Message>();
            CFamilyHelper.CallClangAnalyzer(messages.Add, request, processRunner, testLogger, CancellationToken.None);

            return messages;
        }
    }
}
