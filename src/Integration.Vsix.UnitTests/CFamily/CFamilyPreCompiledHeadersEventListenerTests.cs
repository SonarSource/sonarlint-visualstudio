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
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyPreCompiledHeadersEventListenerTests
    {
        private const string FocusedDocumentFilePath = "c:\\myfile.cpp";

        private Mock<ICLangAnalyzer> clangAnalyzerMock;
        private Mock<IDocumentFocusedEventRaiser> documentFocusedEventRaiserMock;
        private Mock<IScheduler> schedulerMock;
        private IContentType contentTypeMock;

        private CFamilyPreCompiledHeadersEventListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            contentTypeMock = Mock.Of<IContentType>();
            clangAnalyzerMock = new Mock<ICLangAnalyzer>();
            documentFocusedEventRaiserMock = new Mock<IDocumentFocusedEventRaiser>();
            schedulerMock = new Mock<IScheduler>();

            testSubject = new CFamilyPreCompiledHeadersEventListener(clangAnalyzerMock.Object, documentFocusedEventRaiserMock.Object, schedulerMock.Object);
        }

        [TestMethod]
        public void Listen_RegisterToDocumentFocusedEvent()
        {
            testSubject.Listen();

            RaiseDocumentFocusedEvent();

            VerifyJobWasScheduled();
        }

        [TestMethod]
        public void ListenNotCalled_ShouldNotRegisterToDocumentFocusedEvent()
        {
            RaiseDocumentFocusedEvent();

            VerifyJobWasNotScheduled();
            VerifyAnalyzerWasNotCalled();
        }

        [TestMethod]
        public void Dispose_UnregisterFromDocumentFocusedEvent()
        {
            testSubject.Listen();
            testSubject.Dispose();

            RaiseDocumentFocusedEvent();

            VerifyJobWasNotScheduled();
            VerifyAnalyzerWasNotCalled();
        }

        [TestMethod]
        public void OnDocumentFocused_SchedulePchGeneration()
        {
            var cancellationToken = new CancellationTokenSource();

            schedulerMock
                .Setup(x=> x.Schedule(CFamilyPreCompiledHeadersEventListener.PchJobId, It.IsAny<Action<CancellationToken>>(), Timeout.Infinite))
                .Callback((string jobId, Action<CancellationToken> action, int timeout) => action(cancellationToken.Token));

            testSubject.Listen();
            RaiseDocumentFocusedEvent();

            clangAnalyzerMock.Verify(x=> 
                x.ExecuteAnalysis(FocusedDocumentFilePath, 
                null, 
                new List<AnalysisLanguage> {AnalysisLanguage.CFamily},
                null,
                It.Is((IAnalyzerOptions options) => 
                    ((CFamilyAnalyzerOptions)options).CreatePreCompiledHeaders &&
                    ((CFamilyAnalyzerOptions)options).PreCompiledHeadersFilePath == testSubject.pchFilePath),
                cancellationToken.Token));
        }

        private void RaiseDocumentFocusedEvent()
        {
            documentFocusedEventRaiserMock.Raise(x => x.OnDocumentFocused += null, new DocumentFocusedEventArgs(FocusedDocumentFilePath, contentTypeMock));
        }

        private void VerifyJobWasScheduled()
        {
            schedulerMock.Verify(x => x.Schedule(CFamilyPreCompiledHeadersEventListener.PchJobId, It.IsAny<Action<CancellationToken>>(), Timeout.Infinite), Times.Once);
        }

        private void VerifyJobWasNotScheduled()
        {
            schedulerMock.VerifyNoOtherCalls();
        }

        private void VerifyAnalyzerWasNotCalled()
        {
            clangAnalyzerMock.VerifyNoOtherCalls();
        }
    }
}
