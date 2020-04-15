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
using System.ComponentModel.Design;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;
using ThreadHelper = SonarLint.VisualStudio.Integration.UnitTests.ThreadHelper;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class CFamilyReproducerCommandTests
    {
        private Mock<IActiveDocumentLocator> docLocatorMock;
        private Mock<ISonarLanguageRecognizer> languageRecognizerMock;
        private Mock<IAnalysisRequester> analysisRequesterMock;
        private TestLogger logger;

        private ITextDocument ValidTextDocument;

        private MenuCommand testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            docLocatorMock = new Mock<IActiveDocumentLocator>();
            languageRecognizerMock = new Mock<ISonarLanguageRecognizer>();
            analysisRequesterMock = new Mock<IAnalysisRequester>();

            logger = new TestLogger();
            testSubject = CreateCFamilyReproducerCommand(docLocatorMock.Object, languageRecognizerMock.Object,
                analysisRequesterMock.Object, logger);

            ValidTextDocument = CreateValidTextDocument("c:\\subdir1\\file.txt");
        }

        [TestMethod]
        public void Ctor_NullArguments()
        {
            var menuCommandService = new DummyMenuCommandService();

            Action act = () => new CFamilyReproducerCommand(null, docLocatorMock.Object, languageRecognizerMock.Object, analysisRequesterMock.Object, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("menuCommandService");

            act = () => new CFamilyReproducerCommand(menuCommandService, null, languageRecognizerMock.Object, analysisRequesterMock.Object, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeDocumentLocator");

            act = () => new CFamilyReproducerCommand(menuCommandService, docLocatorMock.Object, null, analysisRequesterMock.Object, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("languageRecognizer");

            act = () => new CFamilyReproducerCommand(menuCommandService, docLocatorMock.Object, languageRecognizerMock.Object, null, logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analysisRequester");

            act = () => new CFamilyReproducerCommand(menuCommandService, docLocatorMock.Object, languageRecognizerMock.Object, analysisRequesterMock.Object, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void CommandRegistration()
        {
            testSubject.CommandID.ID.Should().Be(CFamilyReproducerCommand.CommandId);
            testSubject.CommandID.Guid.Should().Be(CFamilyReproducerCommand.CommandSet);
        }

        [TestMethod]
        public void CheckStatus_NoActiveDocument_NotEnabled()
        {
            // Arrange
            SetActiveDocument(null);
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act
            var result = testSubject.OleStatus;

            // Assert
            result.Should().Be((int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE));
            testSubject.Enabled.Should().BeFalse();
            testSubject.Visible.Should().BeFalse();

            VerifyDocumentLocatorCalled();
            VerifyLanguageRecognizerNotCalled();
            VerifyAnalysisNotRequested();
        }

        [TestMethod]
        public void CheckStatus_ActiveDocument_NotCFamilyDocument_NotEnabled()
        {
            // Arrange
            SetActiveDocument(ValidTextDocument, AnalysisLanguage.Javascript);
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act
            var result = testSubject.OleStatus;

            // Assert
            result.Should().Be((int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE));
            testSubject.Enabled.Should().BeFalse();
            testSubject.Visible.Should().BeFalse();

            VerifyDocumentLocatorCalled();
            VerifyLanguageRecognizerCalled();
            VerifyAnalysisNotRequested();
        }

        [TestMethod]
        public void CheckStatus_ActiveDocument_IsCFamilyDocument_Enabled()
        {
            // Arrange
            SetActiveDocument(ValidTextDocument, AnalysisLanguage.Javascript, AnalysisLanguage.CFamily);
            ThreadHelper.SetCurrentThreadAsUIThread();

            // Act
            var result = testSubject.OleStatus;

            // Assert
            result.Should().Be((int)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_INVISIBLE | OLECMDF.OLECMDF_ENABLED));
            testSubject.Enabled.Should().BeTrue();
            testSubject.Visible.Should().BeFalse();

            VerifyDocumentLocatorCalled();
            VerifyLanguageRecognizerCalled();
        }

        [TestMethod]
        public void Execute_ReanalysisTriggered()
        {
            // Arrange
            SetActiveDocument(ValidTextDocument, AnalysisLanguage.CFamily);

            IAnalyzerOptions actualOptions = null;
            string[] actualFilePaths = null;
            analysisRequesterMock.Setup(x => x.RequestAnalysis(It.IsAny<IAnalyzerOptions>(), It.IsAny<string[]>()))
                .Callback<IAnalyzerOptions, string[]>((opts, filePaths) => { actualOptions = opts; actualFilePaths = filePaths; });

            // Act
            testSubject.Invoke();

            // Assert
            logger.AssertOutputStringExists(CFamilyStrings.ReproCmd_ExecutingReproducer);
            actualOptions.Should().BeOfType<CFamilyAnalyzerOptions>();
            ((CFamilyAnalyzerOptions)actualOptions).RunReproducer.Should().BeTrue();
            actualFilePaths.Should().BeEquivalentTo(ValidTextDocument.FilePath);
        }

        [TestMethod]
        public void QueryStatus_NonCriticalErrorSuppressed()
        {
            // Arrange
            docLocatorMock.Setup(x => x.FindActiveDocument()).Throws(new InvalidOperationException("exception xxx"));

            // Act - should not throw
            var _ = testSubject.OleStatus;
            logger.AssertPartialOutputStringExists("exception xxx");
        }

        [TestMethod]
        public void QueryStatus_CriticalErrorNotSuppressed()
        {
            // Arrange
            docLocatorMock.Setup(x => x.FindActiveDocument()).Throws(new StackOverflowException("exception xxx"));
            Action act = () => _ = testSubject.OleStatus;

            // Act
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");
            logger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        [TestMethod]
        public void Execute_NonCriticalErrorSuppressed()
        {
            // Arrange
            docLocatorMock.Setup(x => x.FindActiveDocument()).Throws(new InvalidOperationException("exception xxx"));

            // Act - should not throw
            testSubject.Invoke();
            logger.AssertPartialOutputStringExists("exception xxx");
        }

        [TestMethod]
        public void Execute_CriticalErrorNotSuppressed()
        {
            // Arrange
            docLocatorMock.Setup(x => x.FindActiveDocument()).Throws(new StackOverflowException("exception xxx"));
            Action act = () => testSubject.Invoke();

            // Act 
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("exception xxx");
            logger.AssertPartialOutputStringDoesNotExist("exception xxx");
        }

        private static MenuCommand CreateCFamilyReproducerCommand(IActiveDocumentLocator documentLocator, 
            ISonarLanguageRecognizer languageRecognizer, IAnalysisRequester analysisRequester, ILogger logger)
        {
            var dummyMenuService = new DummyMenuCommandService();
            new CFamilyReproducerCommand(dummyMenuService, documentLocator, languageRecognizer, analysisRequester, logger);

            dummyMenuService.AddedMenuCommands.Count.Should().Be(1);
            return dummyMenuService.AddedMenuCommands[0];
        }

        private static ITextDocument CreateValidTextDocument(string filePath)
        {
            var documentMock = new Mock<ITextDocument>();
            var textBufferMock = new Mock<ITextBuffer>();
            documentMock.Setup(x => x.TextBuffer).Returns(textBufferMock.Object);
            documentMock.Setup(x => x.FilePath).Returns(filePath);
            return documentMock.Object;
        }

        private void SetActiveDocument(ITextDocument document, params AnalysisLanguage[] recognizedLanguages)
        {
            docLocatorMock.Setup(x => x.FindActiveDocument()).Returns(document);
            if (document != null)
            {
                languageRecognizerMock.Setup(x => x.Detect(document, document.TextBuffer)).Returns(recognizedLanguages);
            }
        }

        private void VerifyDocumentLocatorCalled()
            => docLocatorMock.Verify(x => x.FindActiveDocument(), Times.AtLeastOnce);

        private void VerifyLanguageRecognizerNotCalled()
            => languageRecognizerMock.Verify(x => x.Detect(It.IsAny<ITextDocument>(), It.IsAny<ITextBuffer>()), Times.Never);

        private void VerifyLanguageRecognizerCalled()
            => languageRecognizerMock.Verify(x => x.Detect(It.IsAny<ITextDocument>(), It.IsAny<ITextBuffer>()), Times.AtLeastOnce);

        private void VerifyAnalysisNotRequested() =>
            analysisRequesterMock.Verify(x => x.RequestAnalysis(It.IsAny<IAnalyzerOptions>(), It.IsAny<string[]>()), Times.Never);
    }
}
