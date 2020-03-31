using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.TSAnalysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class TypeScriptAnalyzerTests
    {
        private TestLogger logger;

        private const string ScriptLocation = "C:\\Users\\Rita\\Desktop\\eslint-bridge\\bin\\server";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new TestLogger(logToConsole: true);
        }

        [TestMethod]
        public void AnalyzeTSFile_FilePath_SavedCodeIsAnalyzed()
        {
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

            var consumer = ExecuteAnalysis(path);

            consumer.Verify(
                x => x.Accept(path, It.Is<IEnumerable<Issue>>(issues => issues.Count() > 0)), 
                Times.Once);
        }

        [TestMethod]
        public void AnalyzeTSFile_FileContent_EditedCodeIsAnalyzed()
        {
            var savedCode = @"";
            var path = CreateTextFile("test1.ts", savedCode);

            var editedCode = @"// TODO: 123";
            var projectItem = MockEditedCode(editedCode);

            var consumer = ExecuteAnalysis(path, projectItem);

            consumer.Verify(
                x => x.Accept(path, It.Is<IEnumerable<Issue>>(issues => issues.Count() == 1)),
                Times.Once);
        }


        [TestMethod]
        public void AnalyzeTSFile_ParseError_ConsumerNotCalled()
        {
            var code = @"
// TODO: 123

function foo(a) {  // Noncompliant
  if (a) {
}";
            var path = CreateTextFile("test1.ts", code);

            var consumer = ExecuteAnalysis(path);

            consumer.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void AnalyzeTSFile_ParseError_ErrorLogged()
        {
            var code = @"
// TODO: 123

function foo(a) {  // Noncompliant
  if (a) {
}";
            var path = CreateTextFile("test1.ts", code);

            ExecuteAnalysis(path);

            logger.AssertPartialOutputStringExists("Failed to parse file ");
        }

        private string CreateTextFile(string name, string content)
        {
            var fullPath = Path.Combine(TestContext.DeploymentDirectory, name);
            File.WriteAllText(fullPath, content);
            return fullPath;
        }

        private ProjectItem MockEditedCode(string editedCode)
        {
            var startPointMock = new Mock<TextPoint>();
            var endPointMock = new Mock<TextPoint>();
            var editPointMock = new Mock<EditPoint>();

            editPointMock.Setup(x => x.GetText(endPointMock.Object)).Returns(editedCode);
            startPointMock.Setup(x => x.CreateEditPoint()).Returns(editPointMock.Object);

            var textDocumentMock = new Mock<TextDocument>();
            textDocumentMock.Setup(x => x.StartPoint).Returns(startPointMock.Object);
            textDocumentMock.Setup(x => x.EndPoint).Returns(endPointMock.Object);

            var documentMock = new Mock<Document>();
            documentMock.Setup(x => x.Object("")).Returns(textDocumentMock.Object);

            var projectItem = new Mock<ProjectItem>();
            projectItem.Setup(x => x.Document).Returns(documentMock.Object);

            return projectItem.Object;
        }

        private Mock<IIssueConsumer> ExecuteAnalysis(string path, ProjectItem projectItem = null)
        {
            var consumer = new Mock<IIssueConsumer>();

            var analyzer = new TypescriptAnalyzer(logger, 0, ScriptLocation);
            analyzer.ExecuteAnalysis(path, "", new[] {AnalysisLanguage.Typescript}, consumer.Object, projectItem);

            return consumer;
        }
    }
}
