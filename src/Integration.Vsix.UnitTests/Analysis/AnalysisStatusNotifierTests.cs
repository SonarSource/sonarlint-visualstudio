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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisStatusNotifierTests
    {
        private Mock<IStatusBarNotifier> statusBarMock;
        private TestLogger logger;
        private AnalysisStatusNotifier testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            statusBarMock = new Mock<IStatusBarNotifier>();
            logger = new TestLogger();
            testSubject = new AnalysisStatusNotifier(statusBarMock.Object, logger);
        }

        [TestMethod]
        [DataRow("foo-started.cpp", "foo-started.cpp")]
        [DataRow("c:\\test\\foo-started.cpp", "foo-started.cpp")]
        [DataRow("..\\test\\foo-started.cpp", "foo-started.cpp")]
        public void AnalysisStarted_DisplayMessageAndStartSpinner(string filePath, string expectedNotifiedFileName)
        {
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisStarted, expectedNotifiedFileName);

            testSubject.AnalysisStarted(filePath);

            VerifyStatusBarMessageAndIcon(expectedMessage, true);
        }

        [TestMethod]
        public void AnalysisStarted_LogToOutputWindow()
        {
            var filePath = "c:\\test\\foo-started.cpp";
            testSubject.AnalysisStarted(filePath);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisStarted, filePath);
            logger.AssertOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("c:\\test\\foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("..\\test\\foo-finished.cpp", "foo-finished.cpp")]
        public void AnalysisFinished_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFinished, expectedNotifiedFileName);

            testSubject.AnalysisFinished(filePath, 1, TimeSpan.Zero);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFinished_LogToOutputWindow()
        {
            var filePath = "c:\\test\\foo-started.cpp";
            testSubject.AnalysisFinished(filePath, 123, TimeSpan.FromSeconds(6.54321));

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisComplete, filePath, 6.543);
            logger.AssertOutputStringExists(expectedMessage);

            expectedMessage = string.Format($"Found {123} issue(s) for {filePath}");
            logger.AssertOutputStringExists(expectedMessage);

            logger.OutputStrings.Count.Should().Be(2);
        }

        [TestMethod]
        [DataRow("foo-timedout.cpp")]
        [DataRow("c:\\test\\foo-timedout.cpp")]
        [DataRow("..\\test\\foo-timedout.cpp")]
        public void AnalysisCancelled_RemoveMessageAndStopSpinner(string filePath)
        {
            testSubject.AnalysisCancelled(filePath);

            VerifyStatusBarMessageAndIcon("", false);
        }

        [TestMethod]
        public void AnalysisCancelled_LogToOutputWindow()
        {
            var filePath = "c:\\test\\foo-started.cpp";
            testSubject.AnalysisCancelled(filePath);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisAborted, filePath);
            logger.AssertOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            testSubject.AnalysisFailed(filePath, new NullReferenceException("test message"));

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFailed_LogToOutputWindow()
        {
            var exception = new NullReferenceException("test message");
            var filePath = "c:\\test\\foo-started.cpp";
            testSubject.AnalysisFailed(filePath, exception);

            var expectedMessage = string.Format(AnalysisStrings.MSG_AnalysisFailed, filePath, exception);
            logger.AssertOutputStringExists(expectedMessage);
            logger.OutputStrings.Count.Should().Be(1);
        }

        private void VerifyStatusBarMessageAndIcon(string expectedMessage, bool isSpinnerOn)
        {
            statusBarMock.Verify(x=> x.Notify(expectedMessage, isSpinnerOn), Times.Once);
            statusBarMock.VerifyNoOtherCalls();
        }
    }
}
