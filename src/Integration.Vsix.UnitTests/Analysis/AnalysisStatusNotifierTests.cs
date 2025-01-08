﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisStatusNotifierTests
    {
        [TestMethod]
        [DataRow("foo-started.cpp", "foo-started.cpp")]
        [DataRow("c:\\test\\foo-started.cpp", "foo-started.cpp")]
        [DataRow("..\\test\\foo-started.cpp", "foo-started.cpp")]
        public void AnalysisStarted_DisplayMessageAndStartSpinner(string filePath, string expectedNotifiedFileName)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);
            testSubject.AnalysisStarted();

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisStarted, expectedNotifiedFileName);
            VerifyStatusBarMessageAndIcon(statusBarMock, expectedMessage, true);
        }

        [TestMethod]
        public void AnalysisStarted_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);
            testSubject.AnalysisStarted();

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisStarted, filePath, analysisId);
            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void AnalysisProgressed_LogToOutputWindow(bool isIntermediate)
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);
            testSubject.AnalysisProgressed(123, "finding", isIntermediate);

            var expectedMessage = string.Format(AnalysisStrings.MSG_FoundIssues, 123, "finding", filePath, analysisId, !isIntermediate);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.AssertPartialOutputStringExists(analyzerName);
        }

        [TestMethod]
        [DataRow("foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("c:\\test\\foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("..\\test\\foo-finished.cpp", "foo-finished.cpp")]
        public void AnalysisFinished_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();
            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);

            testSubject.AnalysisFinished(TimeSpan.FromSeconds(3));

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFinished, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(statusBarMock, expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFinished_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);

            testSubject.AnalysisFinished(TimeSpan.FromSeconds(6.54321));

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisComplete, filePath, analysisId, 6.543);
            logger.AssertPartialOutputStringExists(expectedMessage);

            logger.AssertPartialOutputStringExists(analyzerName);

            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-timedout.cpp")]
        [DataRow("c:\\test\\foo-timedout.cpp")]
        [DataRow("..\\test\\foo-timedout.cpp")]
        public void AnalysisCancelled_RemoveMessageAndStopSpinner(string filePath)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);

            testSubject.AnalysisCancelled();

            VerifyStatusBarMessageAndIcon(statusBarMock, "", false);
        }

        [TestMethod]
        public void AnalysisCancelled_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);

            testSubject.AnalysisCancelled();

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisAborted, filePath, analysisId);
            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-timedout.cpp")]
        [DataRow("c:\\test\\foo-timedout.cpp")]
        [DataRow("..\\test\\foo-timedout.cpp")]
        public void AnalysisNotReady_RemoveMessageAndStopSpinner(string filePath)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);

            testSubject.AnalysisNotReady("some reason");

            VerifyStatusBarMessageAndIcon(statusBarMock, "", false);
        }

        [TestMethod]
        public void AnalysisNotReady_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            const string reason = "some reason";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);

            testSubject.AnalysisNotReady(reason);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisNotReady, filePath, analysisId, reason);
            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }


        [TestMethod]
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);

            testSubject.AnalysisFailed(new NullReferenceException("test message"));

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(statusBarMock, expectedMessage, false);
        }



        [TestMethod]
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_FailureMessage_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var statusBarMock = new Mock<IStatusBarNotifier>();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), statusBarNotifier: statusBarMock.Object);

            testSubject.AnalysisFailed("test message");

            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            VerifyStatusBarMessageAndIcon(statusBarMock, expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFailed_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);

            var exception = new NullReferenceException("test message");
            testSubject.AnalysisFailed(exception);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisFailed, filePath, analysisId, exception);
            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        public void AnalysisFailed_FailureMessage_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();
            var analysisId = Guid.NewGuid();

            var testSubject = CreateTestSubject(filePath, analysisId, analyzerName, logger: logger);

            testSubject.AnalysisFailed("test message");

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisFailed, filePath, analysisId, "test message");
            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        public void AnalysisFailed_AggregateException_LogToOutputWindow()
        {
            const string analyzerName = "some analyzer";
            const string filePath = "c:\\test\\foo-started.cpp";
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(filePath, Guid.NewGuid(), analyzerName, logger: logger);

            var exception = new AggregateException(
                new List<Exception>
                {
                    new ArgumentNullException("this is a test1"),
                    new NotImplementedException("this is a test2")
                });

            testSubject.AnalysisFailed(exception);

            logger.AssertPartialOutputStringExists(analyzerName);
            logger.AssertPartialOutputStringExists("this is a test1");
            logger.AssertPartialOutputStringExists("this is a test2");
            logger.OutputStrings.Count.Should().Be(1);
        }

        private void VerifyStatusBarMessageAndIcon(Mock<IStatusBarNotifier> statusBarMock, string expectedMessage, bool isSpinnerOn)
        {
            statusBarMock.Verify(x=> x.Notify(expectedMessage, isSpinnerOn), Times.Once);
            statusBarMock.VerifyNoOtherCalls();
        }

        private AnalysisStatusNotifier CreateTestSubject(string filePath,
            Guid? analysisId,
            string analyzerName = "analyzer",
            IStatusBarNotifier statusBarNotifier = null,
            ILogger logger = null)
        {
            statusBarNotifier ??= Mock.Of<IStatusBarNotifier>();
            logger ??= new TestLogger();

            return new AnalysisStatusNotifier(analyzerName, filePath, analysisId, statusBarNotifier, logger);
        }
    }
}
