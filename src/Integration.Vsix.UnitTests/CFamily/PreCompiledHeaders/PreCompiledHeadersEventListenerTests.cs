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
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class PreCompiledHeadersEventListenerTests
    {
        private const string FocusedDocumentFilePath = "c:\\myfile.cpp";
        private readonly IContentType focusedDocumentContentType = Mock.Of<IContentType>();

        private Mock<ICFamilyAnalyzer> cFamilyAnalyzerMock;
        private Mock<IActiveDocumentTracker> activeDocumentTrackerMock;
        private Mock<IScheduler> schedulerMock;
        private Mock<ISonarLanguageRecognizer> languageRecognizerMock;
        private Mock<IPchCacheCleaner> cacheCleanerMock;

        private PreCompiledHeadersEventListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            cFamilyAnalyzerMock = new Mock<ICFamilyAnalyzer>();
            activeDocumentTrackerMock = new Mock<IActiveDocumentTracker>();
            schedulerMock = new Mock<IScheduler>();
            languageRecognizerMock = new Mock<ISonarLanguageRecognizer>();
            cacheCleanerMock = new Mock<IPchCacheCleaner>();

            var environmentSettingsMock = new Mock<IEnvironmentSettings>();
            environmentSettingsMock
                .Setup(x => x.PCHGenerationTimeoutInMs(It.IsAny<int>()))
                .Returns(1);

            testSubject = new PreCompiledHeadersEventListener(cFamilyAnalyzerMock.Object, activeDocumentTrackerMock.Object, schedulerMock.Object, languageRecognizerMock.Object, environmentSettingsMock.Object, cacheCleanerMock.Object);
        }

        [TestMethod]
        public void Ctor_RegisterToDocumentFocusedEvent()
        {
            RaiseDocumentFocusedEvent();

            languageRecognizerMock.Verify(x => x.Detect(FocusedDocumentFilePath, focusedDocumentContentType), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromDocumentFocusedEvent()
        {
            testSubject.Dispose();

            RaiseDocumentFocusedEvent();

            cFamilyAnalyzerMock.VerifyNoOtherCalls();
            schedulerMock.VerifyNoOtherCalls();
            languageRecognizerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_CleanupPchCache()
        {
            testSubject.Dispose();

            cacheCleanerMock.Verify(x=> x.Cleanup(), Times.Once);
        }

        [TestMethod]
        public void Dispose_ExceptionWhenCleaningCache_ExceptionCaught()
        {
            cacheCleanerMock.Setup(x => x.Cleanup()).Throws<FileNotFoundException>();

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Dispose_CriticalExceptionWhenCleaningCache_ExceptionNotCaught()
        {
            cacheCleanerMock.Setup(x => x.Cleanup()).Throws<StackOverflowException>();

            Action act = () => testSubject.Dispose();
            act.Should().ThrowExactly<StackOverflowException>();
        }

        [TestMethod]
        public void OnDocumentFocused_NoLanguagesDetected_PchGenerationNotScheduled()
        {
            SetupDetectedLanguages(Enumerable.Empty<AnalysisLanguage>());

            RaiseDocumentFocusedEvent();

            schedulerMock.VerifyNoOtherCalls();
            cFamilyAnalyzerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnDocumentFocused_LanguageIsUnsupported_PchGenerationNotScheduled()
        {
            var unsupportedLanguages = new List<AnalysisLanguage> {AnalysisLanguage.Javascript};
            
            SetupDetectedLanguages(unsupportedLanguages);

            cFamilyAnalyzerMock.Setup(x => x.IsAnalysisSupported(unsupportedLanguages)).Returns(false).Verifiable();

            RaiseDocumentFocusedEvent();

            schedulerMock.VerifyNoOtherCalls();
            cFamilyAnalyzerMock.Verify();
            cFamilyAnalyzerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnDocumentFocused_LanguageIsSupported_SchedulePchGeneration()
        {
            var supportedLanguages = new List<AnalysisLanguage> { AnalysisLanguage.CFamily };

            SetupDetectedLanguages(supportedLanguages);

            cFamilyAnalyzerMock.Setup(x => x.IsAnalysisSupported(supportedLanguages)).Returns(true);

            var cancellationToken = new CancellationTokenSource();

            schedulerMock
                .Setup(x=> x.Schedule(PreCompiledHeadersEventListener.PchJobId, It.IsAny<Action<CancellationToken>>(), testSubject.pchJobTimeoutInMilliseconds))
                .Callback((string jobId, Action<CancellationToken> action, int timeout) => action(cancellationToken.Token));

            RaiseDocumentFocusedEvent();

            cFamilyAnalyzerMock.Verify(x=> 
                x.ExecuteAnalysis(FocusedDocumentFilePath, 
                supportedLanguages,
                null,
                It.Is((IAnalyzerOptions options) => ((CFamilyAnalyzerOptions)options).CreatePreCompiledHeaders),
                null,
                cancellationToken.Token));
        }

        private void RaiseDocumentFocusedEvent()
        {
            activeDocumentTrackerMock.Raise(x => x.OnDocumentFocused += null, new DocumentFocusedEventArgs(CreateMockTextDocument()));
        }

        private ITextDocument CreateMockTextDocument()
        {
            var textBufferMock = new Mock<ITextBuffer>();
            textBufferMock.Setup(x => x.ContentType).Returns(focusedDocumentContentType);

            var textDocumentMock = new Mock<ITextDocument>();
            textDocumentMock.Setup(x => x.TextBuffer).Returns(textBufferMock.Object);
            textDocumentMock.Setup(x => x.FilePath).Returns(FocusedDocumentFilePath);

            return textDocumentMock.Object;
        }

        private void SetupDetectedLanguages(IEnumerable<AnalysisLanguage> languages)
        {
            languageRecognizerMock
                .Setup(x => x.Detect(FocusedDocumentFilePath, focusedDocumentContentType))
                .Returns(languages);
        }
    }
}
