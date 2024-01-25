/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ActiveDocumentTrackerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_RegisterToSelectionEvents()
        {
            uint cookie = 1234;
            var monitorSelectionMock = new Mock<IVsMonitorSelection>();
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            var testSubject = CreateTestSubject(monitorSelectionMock.Object);

            monitorSelectionMock.Verify(x=> x.AdviseSelectionEvents(testSubject, out cookie), Times.Once);
            monitorSelectionMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_ShouldUnregisterFromSelectionEvents()
        {
            uint cookie = 1234;
            var monitorSelectionMock = new Mock<IVsMonitorSelection>();
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            var testSubject = CreateTestSubject(monitorSelectionMock.Object);
            testSubject.Dispose();

            monitorSelectionMock.Verify(x => x.UnadviseSelectionEvents(cookie), Times.Once);
        }

        [TestMethod]
        [DataRow(VSConstants.VSSELELEMID.SEID_DocumentFrame)]
        [DataRow(VSConstants.VSSELELEMID.SEID_WindowFrame)]
        public void Dispose_ShouldNoLongerRaiseEvents(VSConstants.VSSELELEMID elementId)
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            SimulateElementValueChanged(testSubject, elementId, selectedFrame);

            CheckEventRaised(eventHandler, null);

            eventHandler.Reset();
            testSubject.Dispose();

            SimulateElementValueChanged(testSubject, elementId, selectedFrame);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnCmdUIContextChanged_DoesNotRaiseEvent()
        {
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);
            
            var result = (testSubject as IVsSelectionEvents).OnCmdUIContextChanged(1, 1);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnSelectionChanged_DoesNotRaiseEvent()
        {
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            var result = (testSubject as IVsSelectionEvents).OnSelectionChanged(null, 1, null, null, null, 1, null, null);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        [DataRow(VSConstants.VSSELELEMID.SEID_PropertyBrowserSID)]
        [DataRow(VSConstants.VSSELELEMID.SEID_ResultList)]
        [DataRow(VSConstants.VSSELELEMID.SEID_StartupProject)]
        [DataRow(VSConstants.VSSELELEMID.SEID_UndoManager)]
        [DataRow(VSConstants.VSSELELEMID.SEID_UserContext)]
        public void OnElementValueChanged_UnsupportedElementId_DoesNotRaiseEvent(VSConstants.VSSELELEMID elementId)
        {
            var selectedFrame = Mock.Of<IVsWindowFrame>();

            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            var result = SimulateElementValueChanged(testSubject, elementId, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("not a frame")]
        public void OnElementValueChanged_DocumentElement_NewValueIsNotAFrame_RaisesEventWithNullArg(object newValue)
        {
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, null);
        }

        [TestMethod, Description("Regression test for #2091")]
        public void OnElementValueChanged_DocumentElement_NullTextDocument_RaisesEventWithNullArg()
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            var textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(null as ITextDocument);

            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object, textDocumentProvider: textDocumentProviderMock.Object);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, null);
        }

        [TestMethod]
        public void OnElementValueChanged_DocumentElement_ValidTextDocument_RaisesEvent()
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            var textDocumentMock = Mock.Of<ITextDocument>();
            var textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(textDocumentMock);

            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object, textDocumentProvider: textDocumentProviderMock.Object);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, textDocumentMock);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("not a frame")]
        public void OnElementValueChanged_FrameElement_NewValueIsNotAFrame_DoesNotRaiseEvent(object newValue)
        {
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_WindowFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_NewValueIsNotDocumentFrame_DoesNotRaiseEvent()
        {
            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object);

            var newValue = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Tool);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_WindowFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod, Description("Regression test for #2242")]
        [DataRow(true)]
        [DataRow(false)]
        public void OnElementValueChanged_FrameElement_NewValueIsDocumentFrame_RaisesEvent(bool isNullDocument)
        {
            var newValue = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);

            var textDocumentMock = isNullDocument ? null : Mock.Of<ITextDocument>();
            var textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocumentProviderMock.Setup(x => x.GetFromFrame(newValue)).Returns(textDocumentMock);

            var eventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateTestSubject(eventHandler: eventHandler.Object, textDocumentProvider: textDocumentProviderMock.Object);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_WindowFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, textDocumentMock);
        }

        [TestMethod]
        [DataRow(VSConstants.VSSELELEMID.SEID_DocumentFrame)]
        [DataRow(VSConstants.VSSELELEMID.SEID_WindowFrame)]
        public void OnElementValueChanged_NoEventSubscribers_DoesNotFail(VSConstants.VSSELELEMID elementId)
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);

            var textDocumentMock = Mock.Of<ITextDocument>();
            var textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(textDocumentMock);

            var testSubject = CreateTestSubject(textDocumentProvider: textDocumentProviderMock.Object);

            Func<int> act = () => (testSubject as IVsSelectionEvents).OnElementValueChanged((uint) elementId, "1", selectedFrame);

            act.Should().NotThrow().And.Subject().Should().Be(VSConstants.S_OK);
        }

        private ActiveDocumentTracker CreateTestSubject(IVsMonitorSelection monitorSelection = null,
            ITextDocumentProvider textDocumentProvider = null,
            Action<ActiveDocumentChangedEventArgs> eventHandler = null)
        {
            monitorSelection ??= Mock.Of<IVsMonitorSelection>();
            textDocumentProvider ??= Mock.Of<ITextDocumentProvider>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelection);

            var testSubject = new ActiveDocumentTracker(serviceProviderMock.Object, textDocumentProvider);

            if (eventHandler != null)
            {
                testSubject.ActiveDocumentChanged += (sender, e) => eventHandler(e);
            }

            return testSubject;
        }

        private static IVsWindowFrame CreateWindowFrame(__WindowFrameTypeFlags frameType)
        {
            object frameTypeObj = (int)frameType;

            var frame = new Mock<IVsWindowFrame>();
            frame
                .Setup(x => x.GetProperty((int)__VSFPROPID.VSFPROPID_Type, out frameTypeObj))
                .Returns(VSConstants.S_OK);

            return frame.Object;
        }

        private static int SimulateElementValueChanged(IVsSelectionEvents testSubject, VSConstants.VSSELELEMID elementId, object newValue) =>
            testSubject.OnElementValueChanged((uint)elementId,
                "old value - can be anything",
                newValue);

        private static void CheckEventRaised(Mock<Action<ActiveDocumentChangedEventArgs>> mockEventHandler, ITextDocument expected) =>
            mockEventHandler.Verify(x => x(It.Is((ActiveDocumentChangedEventArgs e) => e.ActiveTextDocument == expected)), Times.Once);

        private static void CheckEventNotRaised(Mock<Action<ActiveDocumentChangedEventArgs>> mockEventHandler) =>
            mockEventHandler.Verify(x => x(It.IsAny<ActiveDocumentChangedEventArgs>()), Times.Never);
    }
}
