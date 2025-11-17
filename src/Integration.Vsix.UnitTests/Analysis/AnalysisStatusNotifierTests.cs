/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisStatusNotifierTests
    {
        private IStatusBarNotifier statusBarNotifier;
        private TestLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            statusBarNotifier = Substitute.For<IStatusBarNotifier>();
            logger = new TestLogger();
        }

        [TestMethod]
        public void Ctor_SetsContext()
        {
            var mockLogger = Substitute.For<ILogger>();

            CreateTestSubject("foo-started.cpp", mockLogger);

            mockLogger.Received(1).ForContext(AnalysisStrings.AnalysisLogContext);
        }

        [TestMethod]
        [DataRow("foo-started.cpp", "foo-started.cpp")]
        [DataRow("c:\\test\\foo-started.cpp", "foo-started.cpp")]
        [DataRow("..\\test\\foo-started.cpp", "foo-started.cpp")]
        public void AnalysisStarted_DisplayMessageAndStartSpinner(string filePath, string expectedNotifiedFileName)
        {
            var testSubject = CreateTestSubject(filePath);
            testSubject.AnalysisStarted();

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisStarted, expectedNotifiedFileName);
            VerifyStatusBarMessageAndIcon(expectedMessage, true);
        }

        [TestMethod]
        public void AnalysisStarted_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";

            var testSubject = CreateTestSubject(filePath);
            testSubject.AnalysisStarted();

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisStarted, Path.GetFileName(filePath));
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AnalysisProgressed_LogToOutputWindow(bool isIntermediate)
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);
            testSubject.AnalysisProgressed(analysisId, 123, "finding", isIntermediate);

            var expectedMessage = string.Format(AnalysisStrings.MSG_FoundIssues, analysisId, 123, "finding", Path.GetFileName(filePath), !isIntermediate);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
        }

        [TestMethod]
        [DataRow("foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("c:\\test\\foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("..\\test\\foo-finished.cpp", "foo-finished.cpp")]
        public void AnalysisFinished_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisFinished(analysisId, TimeSpan.FromSeconds(3));

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFinished, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFinished_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisFinished(analysisId, TimeSpan.FromSeconds(6.54321));

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisComplete, analysisId, Path.GetFileName(filePath), 6.543);
            logger.AssertPartialOutputStringExists(expectedMessage);

            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);

            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-timedout.cpp")]
        [DataRow("c:\\test\\foo-timedout.cpp")]
        [DataRow("..\\test\\foo-timedout.cpp")]
        public void AnalysisCancelled_RemoveMessageAndStopSpinner(string filePath)
        {
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisCancelled(analysisId);

            VerifyStatusBarMessageAndIcon("", false);
        }

        [TestMethod]
        public void AnalysisCancelled_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisCancelled(analysisId);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisAborted, analysisId, Path.GetFileName(filePath));
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-timedout.cpp")]
        [DataRow("c:\\test\\foo-timedout.cpp")]
        [DataRow("..\\test\\foo-timedout.cpp")]
        public void AnalysisNotReady_RemoveMessageAndStopSpinner(string filePath)
        {
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisNotReady(analysisId, "some reason");

            VerifyStatusBarMessageAndIcon("", false);
        }

        [TestMethod]
        public void AnalysisNotReady_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            const string reason = "some reason";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisNotReady(analysisId, reason);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisNotReady, analysisId, Path.GetFileName(filePath), reason);
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisFailed(analysisId, new NullReferenceException("test message"));

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_FailureMessage_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisFailed(analysisId, "test message");

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFailed_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);

            var exception = new NullReferenceException("test message");
            testSubject.AnalysisFailed(analysisId, exception);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisFailed, analysisId, Path.GetFileName(filePath), exception);
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        public void AnalysisFailed_FailureMessage_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath);

            testSubject.AnalysisFailed(analysisId, "test message");

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisFailed, analysisId, Path.GetFileName(filePath), "test message");
            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        public void AnalysisFailed_AggregateException_LogToOutputWindow()
        {
            const string filePath = "c:\\test\\foo-started.cpp";
            var analysisId = Guid.NewGuid();
            var testSubject = CreateTestSubject(filePath);

            var exception = new AggregateException(
                new List<Exception> { new ArgumentNullException("this is a test1"), new NotImplementedException("this is a test2") });

            testSubject.AnalysisFailed(analysisId, exception);

            logger.AssertPartialOutputStringExists(AnalysisStrings.AnalysisLogContext);
            logger.AssertPartialOutputStringExists("this is a test1");
            logger.AssertPartialOutputStringExists("this is a test2");
            logger.OutputStrings.Count.Should().Be(1);
        }

        private void VerifyStatusBarMessageAndIcon(string expectedMessage, bool isSpinnerOn)
        {
            statusBarNotifier.Received(1).Notify(expectedMessage, isSpinnerOn);
            statusBarNotifier.ReceivedCalls().Should().HaveCount(1); //no other calls
        }

        private AnalysisStatusNotifier CreateTestSubject(string filePath, ILogger mockLogger = null) => new([filePath], statusBarNotifier, mockLogger ?? logger);
    }
}
