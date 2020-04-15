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
using Microsoft.VisualStudio;
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
    public class ActiveDocumentLocatorTests
    {
        private Mock<IVsMonitorSelection> monitorSelectionMock;
        private Mock<IVsEditorAdaptersFactoryService> adapterServiceMock;
        private Mock<IVsWindowFrame> frameMock;
        private Mock<IVsTextBuffer> vsTextBufferMock;

        private IVsWindowFrame ValidVsWindowFrame => frameMock.Object;
        private IVsTextBuffer ValidVsTextBuffer => vsTextBufferMock.Object;

        private ActiveDocumentLocator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            monitorSelectionMock = new Mock<IVsMonitorSelection>();
            adapterServiceMock = new Mock<IVsEditorAdaptersFactoryService>();

            testSubject = new ActiveDocumentLocator(monitorSelectionMock.Object, adapterServiceMock.Object);

            frameMock = new Mock<IVsWindowFrame>();
            vsTextBufferMock = new Mock<IVsTextBuffer>();
        }

        [TestMethod]
        public void Find_NoOpenDocuments()
        {
            // Arrange
            Configure(activeFrame: null);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(null);
        }

        [TestMethod]
        public void Find_OpenDocument_NotanIVsWindowFrame()
        {
            // Arrange
            var notAWindowFrame = new object();
            Configure(activeFrame: notAWindowFrame);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(notAWindowFrame);
        }

        [TestMethod]
        public void Find_OpenDocument_FrameDoesNotHaveDocData()
        {
            // Arrange
            Configure(
                activeFrame: ValidVsWindowFrame,
                activeFrameDocDataObject: null);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(ValidVsWindowFrame);
            VerifyFrameGetPropertyCalled(null);
        }

        [TestMethod]
        public void Find_OpenDocument_FrameDocDataIsNotVsTextBuffer()
        {
            // Arrange
            var notVsTextBuffer = new object();

            Configure(
                activeFrame: ValidVsWindowFrame,
                activeFrameDocDataObject: notVsTextBuffer);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(ValidVsWindowFrame);
            VerifyFrameGetPropertyCalled(notVsTextBuffer);
            VerifyAdapterServiceNotCalled();
        }

        [TestMethod]
        public void Find_OpenDocument_FrameDocDataIsTextBuffer_AdapterServiceReturnsNull()
        {
            // Arrange            
            Configure(
                activeFrame: ValidVsWindowFrame,
                activeFrameDocDataObject: ValidVsTextBuffer);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().BeNull();
            VerifyMonitorSelectionServiceCalled(ValidVsWindowFrame);
            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }


        [TestMethod]
        public void Find_OpenDocument_NewTextBuffer_NoTextDoc()
        {
            // Arrange
            var newTextBuffer = CreateValidMefTextBuffer(null);

            Configure(
                activeFrame: ValidVsWindowFrame,
                activeFrameDocDataObject: ValidVsTextBuffer,
                adapterServiceConvertedTextBuffer: newTextBuffer);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().Be(null);

            VerifyMonitorSelectionServiceCalled(ValidVsWindowFrame);
            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }


        [TestMethod]
        public void Find_OpenDocument_NewTextBuffer_HasTextDoc()
        {
            // Arrange
            var newTextDoc = new Mock<ITextDocument>().Object;
            var newTextBuffer = CreateValidMefTextBuffer(newTextDoc);

            Configure(
                activeFrame: ValidVsWindowFrame,
                activeFrameDocDataObject: ValidVsTextBuffer,
                adapterServiceConvertedTextBuffer: newTextBuffer);

            // Act
            var result = testSubject.FindActiveDocument();

            // Assert
            result.Should().Be(newTextDoc);

            VerifyMonitorSelectionServiceCalled(ValidVsWindowFrame);
            VerifyFrameGetPropertyCalled(ValidVsTextBuffer);
            VerifyAdapterServiceCalled(ValidVsTextBuffer);
        }

        private void Configure(
            object activeFrame = null,
            object activeFrameDocDataObject = null,
            ITextBuffer adapterServiceConvertedTextBuffer = null
            )
        {
            monitorSelectionMock.Setup(x => x.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out activeFrame));
            frameMock.Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out activeFrameDocDataObject));
            adapterServiceMock.Setup(x => x.GetDocumentBuffer(activeFrameDocDataObject as IVsTextBuffer)).Returns(adapterServiceConvertedTextBuffer);
        }

        private ITextBuffer CreateValidMefTextBuffer(ITextDocument textDocument)
        {
            var newTextBufferMock = new Mock<ITextBuffer>();

            var bufferProps = new Microsoft.VisualStudio.Utilities.PropertyCollection();
            bufferProps.AddProperty(typeof(ITextDocument), textDocument);
            newTextBufferMock.Setup(x => x.Properties).Returns(bufferProps);

            return newTextBufferMock.Object;
        }

        private void VerifyMonitorSelectionServiceCalled(object obj) =>
            monitorSelectionMock.Verify(x => x.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out obj), Times.Once);

        private void VerifyFrameGetPropertyCalled(object obj) =>
            frameMock.Verify(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out obj),Times.Once);

        private void VerifyAdapterServiceCalled(IVsTextBuffer inputVsBuffer) =>
            adapterServiceMock.Verify(x => x.GetDocumentBuffer(inputVsBuffer), Times.Once);

        private void VerifyAdapterServiceNotCalled() =>
            adapterServiceMock.Verify(x => x.GetDocumentBuffer(It.IsAny<IVsTextBuffer>()), Times.Never);
    }
}
