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

using FluentAssertions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class TextDocumentProviderTests
    {
        private Mock<IVsEditorAdaptersFactoryService> editorAdaptersFactoryServiceMock;
        private Mock<IVsWindowFrame> frameMock;
        private Mock<IVsTextBuffer> vsTextBufferMock;
        private TextDocumentProvider testSubject;

        private IVsWindowFrame ValidVsWindowFrame => frameMock.Object;
        private IVsTextBuffer ValidVsTextBuffer => vsTextBufferMock.Object;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            editorAdaptersFactoryServiceMock = new Mock<IVsEditorAdaptersFactoryService>();

            testSubject = new TextDocumentProvider(editorAdaptersFactoryServiceMock.Object);

            frameMock = new Mock<IVsWindowFrame>();
            vsTextBufferMock = new Mock<IVsTextBuffer>();
        }

        [TestMethod]
        public void GetFromFrame_FrameDoesNotHaveDocData_ReturnsNull()
        {
            // Arrange
            Configure(activeFrameDocDataObject: null);

            // Act
            var result = testSubject.GetFromFrame(ValidVsWindowFrame);

            // Assert
            result.Should().BeNull();
            VerifyFrameGetPropertyCalled(null);
        }

        [TestMethod]
        public void GetFromFrame_FrameDocDataIsNotVsTextBuffer_ReturnsNull()
        {
            // Arrange
            var notVsTextBuffer = new object();

            Configure(activeFrameDocDataObject: notVsTextBuffer);

            // Act
            var result = testSubject.GetFromFrame(ValidVsWindowFrame);

            // Assert
            result.Should().BeNull();
            VerifyFrameGetPropertyCalled(notVsTextBuffer);
            VerifyAdapterServiceNotCalled();
        }

        [TestMethod]
        public void GetFromFrame_FrameDocDataIsTextBuffer_ReturnsNull()
        {
            // Arrange            
            Configure(activeFrameDocDataObject: ValidVsTextBuffer);

            // Act
            var result = testSubject.GetFromFrame(ValidVsWindowFrame);

            // Assert
            result.Should().BeNull();
            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }

        [TestMethod]
        public void GetFromFrame_NewTextBuffer_NoTextDoc_ReturnsNull()
        {
            // Arrange
            var newTextBuffer = CreateValidMefTextBuffer(null);

            Configure(
                activeFrameDocDataObject: ValidVsTextBuffer,
                adapterServiceConvertedTextBuffer: newTextBuffer);

            // Act
            var result = testSubject.GetFromFrame(ValidVsWindowFrame);

            // Assert
            result.Should().Be(null);

            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }

        [TestMethod]
        public void GetFromFrame_NewTextBuffer_HasTextDoc_ReturnsTextDocument()
        {
            // Arrange
            var newTextDoc = new Mock<ITextDocument>().Object;
            var newTextBuffer = CreateValidMefTextBuffer(newTextDoc);

            Configure(
                activeFrameDocDataObject: ValidVsTextBuffer,
                adapterServiceConvertedTextBuffer: newTextBuffer);

            // Act
            var result = testSubject.GetFromFrame(ValidVsWindowFrame);

            // Assert
            result.Should().Be(newTextDoc);

            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }

        private void Configure(object activeFrameDocDataObject = null, ITextBuffer adapterServiceConvertedTextBuffer = null)
        {
            frameMock.Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out activeFrameDocDataObject));
            editorAdaptersFactoryServiceMock.Setup(x => x.GetDocumentBuffer(activeFrameDocDataObject as IVsTextBuffer)).Returns(adapterServiceConvertedTextBuffer);
        }

        private ITextBuffer CreateValidMefTextBuffer(ITextDocument textDocument)
        {
            var newTextBufferMock = new Mock<ITextBuffer>();

            var bufferProps = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            bufferProps.AddProperty(typeof(ITextDocument), textDocument);
            newTextBufferMock.Setup(x => x.Properties).Returns(bufferProps);

            return newTextBufferMock.Object;
        }

        private void VerifyFrameGetPropertyCalled(object obj) =>
            frameMock.Verify(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out obj), Times.Once);

        private void VerifyAdapterServiceCalled(IVsTextBuffer inputVsBuffer) =>
            editorAdaptersFactoryServiceMock.Verify(x => x.GetDocumentBuffer(inputVsBuffer), Times.Once);

        private void VerifyAdapterServiceNotCalled() =>
            editorAdaptersFactoryServiceMock.Verify(x => x.GetDocumentBuffer(It.IsAny<IVsTextBuffer>()), Times.Never);
    }
}
