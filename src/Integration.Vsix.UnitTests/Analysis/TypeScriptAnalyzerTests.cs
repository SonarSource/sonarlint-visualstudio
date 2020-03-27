using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.TSAnalysis;


namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class TypeScriptAnalyzerTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void AnalyzeTSFile()
        {
            var consumer = new DummyConsumer();

            const int port = 50487;
            var analyzer = new TypescriptAnalyzer(port, new TestLogger(logToConsole: true));
            
            // actually passing code rather than a path
            var code = @"

let ss1 = ""123"";
let ss2 = ""123"";
let ss3 = ""123"";
let ss4 = ""123"";
let ss5 = ""123"";
let ss6 = ""123"";

// TODO: 123

// TODO

function foo(a) {  // Noncompliant
  let b = 12;
  if (a) {
    return b;
  }
  return b;
}

";

            var path = CreateTextFile("test1.ts", code);

            analyzer.ExecuteAnalysis(path, "", new [] { AnalysisLanguage.Typescript }, consumer, null);


            consumer.Issues.Should().NotBeEmpty();
        }

        private string CreateTextFile(string name, string content)
        {
            var fullPath = Path.Combine(TestContext.DeploymentDirectory, name);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        private class DummyConsumer : IIssueConsumer
        {
            public string Path { get; private set; }
            public IEnumerable<Issue> Issues { get; private set; }

            public void Accept(string path, IEnumerable<Issue> issues)
            {
                Path = path;
                Issues = issues;
            }
        }
    }
}
