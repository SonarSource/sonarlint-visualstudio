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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Helpers.DocumentEvents;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class DocumentFocusedEventRaiserTests
    {
        private Mock<IVsMonitorSelection> monitorSelectionMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<Action<DocumentFocusedEventArgs>> mockEventHandler;
        private DocumentFocusedEventRaiser testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            monitorSelectionMock = new Mock<IVsMonitorSelection>();

            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelectionMock.Object);

            mockEventHandler = new Mock<Action<DocumentFocusedEventArgs>>();

            testSubject = new DocumentFocusedEventRaiser(serviceProviderMock.Object);
            testSubject.OnDocumentFocused += (sender, e) => mockEventHandler.Object(e);
        }

        [TestMethod]
        public void Ctor_ShouldRegisterToSelectionEvents()
        {
            uint cookie = 1234;
            monitorSelectionMock.Reset();
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            testSubject = new DocumentFocusedEventRaiser(serviceProviderMock.Object);

            monitorSelectionMock.Verify(x=> x.AdviseSelectionEvents(testSubject, out cookie), Times.Once);
            monitorSelectionMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_ShouldUnregisterFromSelectionEvents()
        {
            uint cookie = 1234;
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            testSubject = new DocumentFocusedEventRaiser(serviceProviderMock.Object);
            testSubject.Dispose();

            monitorSelectionMock.Verify(x => x.UnadviseSelectionEvents(cookie), Times.Once);
        }

        [TestMethod]
        public void Dispose_ShouldNoLongerRaiseEvents()
        {
            var selectedFrame = CreateDocumentFrame("c:\\test.cpp");

            testSubject.Dispose();
            testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);

            mockEventHandler.Verify(x => x(It.IsAny<DocumentFocusedEventArgs>()), Times.Never);
        }

        [TestMethod]
        public void OnCmdUIContextChanged_DoesNotRaiseEvent()
        {
            var result = testSubject.OnCmdUIContextChanged(1, 1);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnSelectionChanged_DoesNotRaiseEvent()
        {
            var result = testSubject.OnSelectionChanged(null, 1, null, null, null, 1, null, null);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnElementValueChanged_NewValueIsNull_DoesNotRaiseEvent()
        {
            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", null);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnElementValueChanged_NonWindowFrameElement_DoesNotRaiseEvent()
        {
            var selectedFrame = CreateDocumentFrame("c:\\test.cpp");

            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, "1", selectedFrame.Object);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
            selectedFrame.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_CantQueryFrameType_DoesNotRaiseEvent()
        {
            var selectedFrame = CreateFrameWithoutType();

            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
            VerifyDocumentPathNotQueried(selectedFrame);
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_ToolFrame_DoesNotRaiseEvent()
        {
            var selectedFrame = CreateToolFrame();

            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
            VerifyDocumentPathNotQueried(selectedFrame);
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_DocumentFrame_CantQueryDocumentName_DoesNotRaiseEvent()
        {
            var selectedFrame = CreateDocumentFrame("c:\\test.cpp", false);

            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_DocumentFrame_SucceededQueryingDocumentName_RaisesEventWithDocumentName()
        {
            var selectedFrame = CreateDocumentFrame("c:\\test.cpp");

            var result = testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.Verify(x=> x(It.Is((DocumentFocusedEventArgs e) => e.DocumentFilePath == "c:\\test.cpp")), Times.Once);
        }

        [TestMethod]
        public void OnElementValueChanged_ValidDocumentFrame_NoEventSubscribers_DoesNotFail()
        {
            testSubject = new DocumentFocusedEventRaiser(serviceProviderMock.Object);

            var selectedFrame = CreateDocumentFrame("c:\\test.cpp");

            Func<int> act = () => testSubject.OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame.Object);

            act.Should().NotThrow().And.Subject().Should().Be(VSConstants.S_OK);
        }

        private Mock<IVsWindowFrame> CreateFrameWithoutType()
        {
            return CreateMockFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document, false);
        }

        private Mock<IVsWindowFrame> CreateToolFrame()
        {
            return CreateMockFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Tool, true);
        }

        private Mock<IVsWindowFrame> CreateDocumentFrame(string documentPath, bool canQueryDocumentPath = true)
        {
            var frame = CreateMockFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document, true);
            var path = (object)documentPath;
            var queryDocumentPathResult = canQueryDocumentPath ? VSConstants.S_OK : VSConstants.E_FAIL;

            frame
                .Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out path))
                .Returns(queryDocumentPathResult);

            return frame;
        }

        private Mock<IVsWindowFrame> CreateMockFrame(__WindowFrameTypeFlags frameType, bool canQueryFrameType)
        {
            var frame = new Mock<IVsWindowFrame>();
            var type = (object)(int)frameType;
            var queryFrameTypeResult = canQueryFrameType ? VSConstants.S_OK : VSConstants.E_FAIL;

            frame
                .Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out type))
                .Returns(queryFrameTypeResult);

            return frame;
        }

        private void VerifyDocumentPathNotQueried(Mock<IVsWindowFrame> frame)
        {
            var value = It.IsAny<object>();
            frame.Verify(x=> x.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out value), Times.Never);
        }
    }
}
