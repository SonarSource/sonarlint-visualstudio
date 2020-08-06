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
        private AnalysisStatusNotifier testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            statusBarMock = new Mock<IStatusBarNotifier>();
            testSubject = new AnalysisStatusNotifier(statusBarMock.Object);
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
        [DataRow("foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("c:\\test\\foo-finished.cpp", "foo-finished.cpp")]
        [DataRow("..\\test\\foo-finished.cpp", "foo-finished.cpp")]
        public void AnalysisFinished_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFinished, expectedNotifiedFileName);

            testSubject.AnalysisFinished(filePath);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
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
        [DataRow("foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("c:\\test\\foo-failed.cpp", "foo-failed.cpp")]
        [DataRow("..\\test\\foo-failed.cpp", "foo-failed.cpp")]
        public void AnalysisFailed_DisplayMessageAndStopSpinner(string filePath, string expectedNotifiedFileName)
        {
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, expectedNotifiedFileName);

            testSubject.AnalysisFailed(filePath);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        private void VerifyStatusBarMessageAndIcon(string expectedMessage, bool isSpinnerOn)
        {
            statusBarMock.Verify(x=> x.Notify(expectedMessage, isSpinnerOn), Times.Once);
            statusBarMock.VerifyNoOtherCalls();
        }
    }
}
