using System;
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
            testsDataDirectory = Path.Combine(Environment.CurrentDirectory, "CFamily\\IntegrationTests\\");
        }

        [TestMethod]
        [DataRow("CLangAnalyzerTestFile_NoIssues_EmptyFile")]
        [DataRow("CLangAnalyzerTestFile_OneIssue")]
        [DataRow("CLangAnalyzerTestFile_TwoIssues")]
        public void CallAnalyzer_IntegrationTest(string testCaseFileName)
        {
            var testedFile = Path.Combine(testsDataDirectory, testCaseFileName + ".txt");

            var request = GetRequest(testedFile);
            var expectedResponse = GetExpectedResponse(testCaseFileName, testedFile);

            var response = InvokeAnalyzer(request);
            response.Should().BeEquivalentTo(expectedResponse);
        }

        private Request GetRequest(string testedFile)
        {
            var requestJson = File.ReadAllText(testsDataDirectory + "CLangAnalyzerRequestTemplate.json");
            var request = JsonConvert.DeserializeObject<Request>(requestJson);
            request.File = testedFile;
            
            return request;
        }

        private static Response InvokeAnalyzer(Request request)
        {
            var testLogger = new TestLogger();
            var processRunner = new ProcessRunner(new ConfigurableSonarLintSettings(), testLogger);
            var response = CFamilyHelper.CallClangAnalyzer(request, processRunner, testLogger, CancellationToken.None);
            
            return response;
        }

        private Response GetExpectedResponse(string testFileName, string testedFile)
        {
            var expectedResponseJson = File.ReadAllText(Path.Combine(testsDataDirectory, testFileName + "_response.json"));
            var expectedResponse = JsonConvert.DeserializeObject<Response>(expectedResponseJson);

            foreach (var expectedResponseMessage in expectedResponse.Messages)
            {
                expectedResponseMessage.Filename = testedFile;
            }

            return expectedResponse;
        }
    }
}
