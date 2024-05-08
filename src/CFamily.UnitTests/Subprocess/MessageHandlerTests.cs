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

using System.Linq;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.CFamily.SubProcess.UnitTests
{
    [TestClass]
    public class MessageHandlerTests
    {
        [TestMethod]
        public void TestIsIssueForActiveRule()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("any")
                .AddRule("rule1", isActive: true)
                .AddRule("rule2", isActive: false);

            // 1. Match - active
            var message = new Message("rule1", "filename", 0, 0, 0, 0, "msg", false, null, Array.Empty<Fix>());
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeTrue();

            // 2. Match - not active
            message = new Message("rule2", "filename", 0, 0, 0, 0, "msg", false, null, Array.Empty<Fix>());
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 3. No match - case-sensitivity
            message = new Message("RULE1", "filename", 0, 0, 0, 0, "msg", false, null, Array.Empty<Fix>());
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 4. No match
            message = new Message("xxx", "filename", 0, 0, 0, 0, "msg", false, null, Array.Empty<Fix>());
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();
        }

        [TestMethod]
        public void HandleMessage_NoMessages_AnalysisSucceeds()
        {
            var context = new MessageHandlerTestContext();

            var testSubject = context.CreateTestSubject();

            testSubject.AnalysisSucceeded.Should().BeTrue();
        }

        [TestMethod]
        public void HandleMessage_IssueProcessing_InactiveRulesAreIgnored_ActiveRulesAreNotIgnored()
        {
            const string fileName = "c:\\data\\aaa\\bbb\\file.txt";
            var inactiveRuleMessage = CreateMessage("inactiveRule", fileName);
            var activeRuleMessage = CreateMessage("activeRule", fileName);

            var context = new MessageHandlerTestContext()
                .SetRequestFilePath(fileName)
                .AddRule("inactiveRule", isActive: false)
                .AddRule("activeRule", isActive: true);

            var convertedActiveMessage = Mock.Of<IAnalysisIssue>();
            context.IssueConverter
                .Setup(x => x.Convert(activeRuleMessage, context.AnalysisLanguageKey, context.RulesConfig))
                .Returns(convertedActiveMessage);

            var testSubject = context.CreateTestSubject();

            // Stream the inactive rule message to the analyzer
            // - should not be passed to the consumer or converted
            testSubject.HandleMessage(inactiveRuleMessage);

            context.AssertNoIssuesProcessed();

            // Now stream an active rule message
            testSubject.HandleMessage(activeRuleMessage);

            testSubject.AnalysisSucceeded.Should().BeTrue();
            testSubject.IssueCount.Should().Be(1);
            context.IssueConverter.VerifyAll();

            var suppliedIssues = (IEnumerable<IAnalysisIssue>)context.IssueConsumer.Invocations[0].Arguments[1];
            suppliedIssues.Count().Should().Be(1);
            suppliedIssues.First().Should().Be(convertedActiveMessage);
        }

        [TestMethod]
        [DataRow("c:\\Analyzedfile.txt")] // exact match
        [DataRow("C:\\ANALYZEDFILE.TXT")] // different case
        [DataRow("C:\\AAA\\..\\ANALYZEDFILE.TXT")] // logically the same file path
        public void HandleMessage_IssuesForAnalyzedFileAreNotIgnored(string fileNameInMessage)
        {
            const string analyzedFile = "c:\\Analyzedfile.txt";
            var analyzedFileMessage = CreateMessage("activeRule", fileNameInMessage);

            var context = new MessageHandlerTestContext()
                .SetRequestFilePath(analyzedFile)
                .AddRule("activeRule", isActive: true);

            var testSubject = context.CreateTestSubject();

            // Process the message
            testSubject.HandleMessage(analyzedFileMessage);

            testSubject.AnalysisSucceeded.Should().BeTrue();
            context.IssueConverter.Invocations.Count.Should().Be(1);
            context.IssueConsumer.Invocations.Count.Should().Be(1);

            context.IssueConsumer.Verify(x => x.Accept(analyzedFile, It.IsAny<IEnumerable<IAnalysisIssue>>()));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("another file")]
        [DataRow("D:\\Analyzedfile.txt")] // correct file name, wrong drive
        public void HandleMessage_IssuesForOtherFilesAreIgnored(string messageFileName)
        {
            const string analyzedFile = "c:\\Analyzedfile.txt";
            var otherFileMessage = CreateMessage("activeRule", messageFileName);

            var context = new MessageHandlerTestContext()
                .SetRequestFilePath(analyzedFile)
                .AddRule("activeRule", isActive: true);

            var testSubject = context.CreateTestSubject();

            // Process the message
            testSubject.HandleMessage(otherFileMessage);

            testSubject.AnalysisSucceeded.Should().BeTrue(); // analysis still succeeded, even though no issues reported.
            context.AssertNoIssuesProcessed();
        }

        [TestMethod]
        [DataRow("internal.InvalidInput", "MsgHandler_ReportInvalidInput")]
        [DataRow("internal.UnexpectedFailure", "MsgHandler_ReportUnexpectedFailure")]
        [DataRow("internal.UnsupportedConfig", "MsgHandler_ReportUnsupportedConfiguration")]
        public void HandleMessage_InternalErrorMessage_IsReportedAndAnalysisFails(string internalRuleKey, string expectedResourceMessageName)
        {
            // Note: this test assumes that all of the internal rule error messages have a single placeholder
            // into which the message text is inserted.
            string logMessageFormat = CFamilyStrings.ResourceManager.GetString(expectedResourceMessageName);
            var expectedLogMessage = string.Format(logMessageFormat, "XXX internal error XXX");

            var internalMessage = CreateMessage(internalRuleKey, text: "XXX internal error XXX");

            var context = new MessageHandlerTestContext()
                .AddRule("S123", isActive: true);
            var testSubject = context.CreateTestSubject();

            // Act
            testSubject.HandleMessage(internalMessage);

            testSubject.AnalysisSucceeded.Should().BeFalse();
            context.Logger.AssertOutputStringExists(expectedLogMessage);
            context.AssertNoIssuesProcessed();
        }

        [TestMethod]
        [DataRow("internal.fileDependency")] // real property
        [DataRow("internal.something")] // fake property
        [DataRow("internal.InvalidInputtttt")] // testing that it takes any starts with
        public void HandleMessage_UnknownInternalRules_IsIgnored(string ruleId)
        {
            var internalMessage = CreateMessage(ruleId, text: "c:\\file.txt");

            var context = new MessageHandlerTestContext()
                // The message file name matches the file being analyzed, but should be ignored anyway
                // because of the rule key
                .SetRequestFilePath("c:\\file.txt");

            var testSubject = context.CreateTestSubject();

            // Act
            testSubject.HandleMessage(internalMessage);

            testSubject.AnalysisSucceeded.Should().BeTrue();
            context.Logger.AssertNoOutputMessages();
            context.AssertNoIssuesProcessed();
        }

        private static Message CreateMessage(string ruleId, string fileName = "any file", string text = "any text") =>
            new(ruleId, fileName, -1, -1, -1, -1, text, false, null, Array.Empty<Fix>());


        private class MessageHandlerTestContext
        {
            public Mock<IIssueConsumer> IssueConsumer { get; } = new Mock<IIssueConsumer>();
            public Mock<ICFamilyIssueToAnalysisIssueConverter> IssueConverter { get; } = new Mock<ICFamilyIssueToAnalysisIssueConverter>();
            public TestLogger Logger { get; } = new TestLogger(logToConsole: true);

            private string requestFilePath = "any.txt";
            private const string languageKey = "c";

            public string AnalysisLanguageKey { get; } = languageKey;

            public DummyCFamilyRulesConfig RulesConfig { get; } = new DummyCFamilyRulesConfig(languageKey);

            public MessageHandler TestSubject { get; set; }

            public MessageHandlerTestContext SetRequestFilePath (string fileToAnalyze)
            {
                requestFilePath = fileToAnalyze;
                return this;
            }

            public MessageHandlerTestContext AddRule(string ruleKey, bool isActive)
            {
                RulesConfig.AddRule(ruleKey, isActive);
                return this;
            }

            public MessageHandler CreateTestSubject()
            {
                if (TestSubject != null)
                {
                    throw new InvalidOperationException("Test setup error: TestSubject has already been created");
                }

                var request = CreateRequest(requestFilePath, AnalysisLanguageKey, RulesConfig);

                TestSubject = new MessageHandler(request, IssueConsumer.Object, IssueConverter.Object, Logger);
                return TestSubject;
            }

            public void AssertNoIssuesProcessed()
            {
                TestSubject.IssueCount.Should().Be(0);
                IssueConverter.Invocations.Count.Should().Be(0);
                IssueConsumer.Invocations.Count.Should().Be(0);
            }

            private static IRequest CreateRequest(string file = null, string language = null, ICFamilyRulesConfig rulesConfiguration = null)
            {
                var request = new Mock<IRequest>();
                var context = new RequestContext(language, rulesConfiguration, file, null, null, CFamilyShared.IsHeaderFileExtension(file));
                request.SetupGet(x => x.Context).Returns(context);
                return request.Object;
            }
        }
    }
}
