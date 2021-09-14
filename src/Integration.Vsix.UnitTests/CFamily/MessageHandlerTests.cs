/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
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
            var message = new Message("rule1", "filename", 0, 0, 0, 0, "msg", false, null);
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeTrue();

            // 2. Match - not active
            message = new Message("rule2", "filename", 0, 0, 0, 0, "msg", false, null);
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 3. No match - case-sensitivity
            message = new Message("RULE1", "filename", 0, 0, 0, 0, "msg", false, null);
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();

            // 4. No match
            message = new Message("xxx", "filename", 0, 0, 0, 0, "msg", false, null);
            MessageHandler.IsIssueForActiveRule(message, rulesConfig).Should().BeFalse();
        }

        [TestMethod]
        public void HandleMessage_IssueProcessing_InactiveRulesAreIgnored_ActiveRulesAreNotIgnored()
        {
            const string fileName = "c:\\data\\aaa\\bbb\\file.txt";
            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("inactiveRule", isActive: false)
                .AddRule("activeRule", isActive: true);

            var request = CreateRequest
            (
                file: fileName,
                rulesConfiguration: rulesConfig,
                language: rulesConfig.LanguageKey
            );

            var inactiveRuleMessage = CreateMessage("inactiveRule", fileName);
            var activeRuleMessage = CreateMessage("activeRule", fileName);

            var issueConverter = new Mock<ICFamilyIssueToAnalysisIssueConverter>();
            var convertedActiveMessage = Mock.Of<IAnalysisIssue>();
            issueConverter
                .Setup(x => x.Convert(activeRuleMessage, request.Context.CFamilyLanguage, rulesConfig))
                .Returns(convertedActiveMessage);

            var issueConsumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(request, issueConsumer.Object, issueConverter.Object);

            // Stream the inactive rule message to the analyzer
            // - should not be passed to the consumer or converted
            testSubject.HandleMessage(inactiveRuleMessage);
            
            testSubject.IssueCount.Should().Be(0);
            issueConverter.Invocations.Count.Should().Be(0);
            issueConsumer.Invocations.Count.Should().Be(0);

            // Now stream an active rule message
            testSubject.HandleMessage(activeRuleMessage);

            testSubject.IssueCount.Should().Be(1);
            issueConverter.VerifyAll();
            issueConsumer.VerifyAll();

            var suppliedIssues = (IEnumerable<IAnalysisIssue>)issueConsumer.Invocations[0].Arguments[1];
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

            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("activeRule", isActive: true);

            var request = CreateRequest
            (
                file: analyzedFile,
                rulesConfiguration: rulesConfig,
                language: rulesConfig.LanguageKey
            );
            
            var analyzedFileMessage = CreateMessage("activeRule", fileNameInMessage);

            var issueConverter = new Mock<ICFamilyIssueToAnalysisIssueConverter>();
            var issueConsumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(request, issueConsumer.Object, issueConverter.Object);

            // Process the message
            testSubject.HandleMessage(analyzedFileMessage);
            issueConverter.Invocations.Count.Should().Be(1);
            issueConsumer.Invocations.Count.Should().Be(1);

            issueConsumer.Verify(x => x.Accept(analyzedFile, It.IsAny<IEnumerable<IAnalysisIssue>>()));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("another file")]
        [DataRow("D:\\Analyzedfile.txt")] // correct file name, wrong drive
        public void HandleMessage_IssuesForOtherFilesAreIgnored(string messageFileName)
        {
            const string analyzedFile = "c:\\Analyzedfile.txt";

            var rulesConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("activeRule", isActive: true);

            var request = CreateRequest
            (
                file: analyzedFile,
                rulesConfiguration: rulesConfig,
                language: rulesConfig.LanguageKey
            );

            var otherFileMessage = CreateMessage("activeRule", messageFileName);
            var issueConverter = new Mock<ICFamilyIssueToAnalysisIssueConverter>();
            var issueConsumer = new Mock<IIssueConsumer>();

            var testSubject = CreateTestSubject(request, issueConsumer.Object, issueConverter.Object);

            // Process the message
            testSubject.HandleMessage(otherFileMessage);
            testSubject.IssueCount.Should().Be(0);
            issueConverter.Invocations.Count.Should().Be(0);
            issueConsumer.Invocations.Count.Should().Be(0);
        }

        private static MessageHandler CreateTestSubject(IRequest request,
            IIssueConsumer issueConsumer = null,
            ICFamilyIssueToAnalysisIssueConverter issueConverter = null)
        {
            issueConsumer ??= Mock.Of<IIssueConsumer>();
            issueConverter ??= Mock.Of<ICFamilyIssueToAnalysisIssueConverter>();

            return new MessageHandler(request, issueConsumer, issueConverter);
        }

        private static IRequest CreateRequest(string file = null, string language = null, ICFamilyRulesConfig rulesConfiguration = null)
        {
            var request = new Mock<IRequest>();
            var context = new RequestContext(language, rulesConfiguration, file, null, null);
            request.SetupGet(x => x.Context).Returns(context);
            return request.Object;
        }

        private static Message CreateMessage(string ruleId, string fileName = "any file") =>
            new Message(ruleId, fileName, -1, -1, -1, -1, "any text", false, null);
    }
}
