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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisStatusNotifierTests
    {
        private Mock<IVsStatusbar> statusBarMock;
        private AnalysisStatusNotifier testSubject;
        private object statusIcon;

        [TestInitialize]
        public void TestInitialize()
        {
            statusIcon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_General;
            statusBarMock = new Mock<IVsStatusbar>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(IVsStatusbar))).Returns(statusBarMock.Object);

            testSubject = new AnalysisStatusNotifier(serviceProviderMock.Object);

            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void AnalysisStarted_DisplayMessageAndStartSpinner()
        {
            const string fileName = "foo-started.cpp";
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisStarted, fileName);

            testSubject.AnalysisStarted(fileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, true);
        }

        [TestMethod]
        public void AnalysisFinished_DisplayMessageAndStopSpinner()
        {
            const string fileName = "foo-finished.cpp";
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisEnded, fileName);

            testSubject.AnalysisFinished(fileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisCancelled_DisplayMessageAndStopSpinner()
        {
            const string fileName = "foo-cancelled.cpp";
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisCancelled, fileName);

            testSubject.AnalysisCancelled(fileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        [TestMethod]
        public void AnalysisFailed_DisplayMessageAndStopSpinner()
        {
            const string fileName = "foo-failed.cpp";
            var expectedMessage = string.Format(AnalysisStrings.Notifier_AnalysisFailed, fileName);

            testSubject.AnalysisFailed(fileName);

            VerifyStatusBarMessageAndIcon(expectedMessage, false);
        }

        private void VerifyStatusBarMessageAndIcon(string expectedMessage, bool isSpinnerOn)
        {
            statusBarMock.Verify(x => x.SetText(expectedMessage), Times.Once);
            statusBarMock.Verify(x => x.Animation(isSpinnerOn ? 1 : 0, ref statusIcon), Times.Once);
            statusBarMock.VerifyNoOtherCalls();
        }
    }
}
