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
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
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
        private IContentType contentTypeMock;

        private Mock<ICLangAnalyzer> clangAnalyzerMock;
        private Mock<IActiveDocumentTracker> activeDocumentTrackerMock;
        private Mock<IScheduler> schedulerMock;
        private Mock<ISonarLanguageRecognizer> languageRecognizerMock;

        private CFamilyPreCompiledHeadersEventListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            clangAnalyzerMock = new Mock<ICLangAnalyzer>();
            activeDocumentTrackerMock = new Mock<IActiveDocumentTracker>();
            schedulerMock = new Mock<IScheduler>();
            languageRecognizerMock = new Mock<ISonarLanguageRecognizer>();

            testSubject = new CFamilyPreCompiledHeadersEventListener(clangAnalyzerMock.Object, activeDocumentTrackerMock.Object, schedulerMock.Object, languageRecognizerMock.Object);
        }

        [TestMethod]
        public void Listen_RegisterToDocumentFocusedEvent()
        {
            testSubject.Listen();

            RaiseDocumentFocusedEvent();

            languageRecognizerMock.Verify(x => x.Detect(FocusedDocumentFilePath, contentTypeMock), Times.Once);
        }

        [TestMethod]
        public void ListenNotCalled_ShouldNotRegisterToDocumentFocusedEvent()
        {
            RaiseDocumentFocusedEvent();

            clangAnalyzerMock.VerifyNoOtherCalls();
            schedulerMock.VerifyNoOtherCalls();
            languageRecognizerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromDocumentFocusedEvent()
        {
            testSubject.Listen();
            testSubject.Dispose();

            RaiseDocumentFocusedEvent();

            clangAnalyzerMock.VerifyNoOtherCalls();
            schedulerMock.VerifyNoOtherCalls();
            languageRecognizerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnDocumentFocused_NoLanguagesDetected_PchGenerationNotScheduled()
        {
            languageRecognizerMock
                .Setup(x => x.Detect(FocusedDocumentFilePath, contentTypeMock))
                .Returns(Enumerable.Empty<AnalysisLanguage>());

            testSubject.Listen();
            RaiseDocumentFocusedEvent();

            schedulerMock.VerifyNoOtherCalls();
            clangAnalyzerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnDocumentFocused_LanguageIsUnsupported_PchGenerationNotScheduled()
        {
            var unsupportedLanguages = new List<AnalysisLanguage> {AnalysisLanguage.Javascript};

            languageRecognizerMock
                .Setup(x => x.Detect(FocusedDocumentFilePath, contentTypeMock))
                .Returns(unsupportedLanguages);

            clangAnalyzerMock.Setup(x => x.IsAnalysisSupported(unsupportedLanguages)).Returns(false).Verifiable();

            testSubject.Listen();
            RaiseDocumentFocusedEvent();

            schedulerMock.VerifyNoOtherCalls();
            clangAnalyzerMock.Verify();
            clangAnalyzerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnDocumentFocused_LanguageIsSupported_SchedulePchGeneration()
        {
            var supportedLanguages = new List<AnalysisLanguage> { AnalysisLanguage.CFamily };

            languageRecognizerMock
                .Setup(x => x.Detect(FocusedDocumentFilePath, contentTypeMock))
                .Returns(supportedLanguages);

            clangAnalyzerMock.Setup(x => x.IsAnalysisSupported(supportedLanguages)).Returns(true);

            var cancellationToken = new CancellationTokenSource();

            schedulerMock
                .Setup(x=> x.Schedule(CFamilyPreCompiledHeadersEventListener.PchJobId, It.IsAny<Action<CancellationToken>>(), Timeout.Infinite))
                .Callback((string jobId, Action<CancellationToken> action, int timeout) => action(cancellationToken.Token));

            testSubject.Listen();
            RaiseDocumentFocusedEvent();

            clangAnalyzerMock.Verify(x=> 
                x.ExecuteAnalysis(FocusedDocumentFilePath, 
                null,
                supportedLanguages,
                null,
                It.Is((IAnalyzerOptions options) => 
                    ((CFamilyAnalyzerOptions)options).CreatePreCompiledHeaders &&
                    ((CFamilyAnalyzerOptions)options).PreCompiledHeadersFilePath == testSubject.pchFilePath),
                cancellationToken.Token));
        }

        private void RaiseDocumentFocusedEvent()
        {
            activeDocumentTrackerMock.Raise(x => x.OnDocumentFocused += null, new DocumentFocusedEventArgs(CreateMockTextDocument()));
        }

        private ITextDocument CreateMockTextDocument()
        {
            contentTypeMock = Mock.Of<IContentType>();

            var textBufferMock = new Mock<ITextBuffer>();
            textBufferMock.Setup(x => x.ContentType).Returns(contentTypeMock);

            var textDocumentMock = new Mock<ITextDocument>();
            textDocumentMock.Setup(x => x.TextBuffer).Returns(textBufferMock.Object);

            return textDocumentMock.Object;
        }
    }
}
