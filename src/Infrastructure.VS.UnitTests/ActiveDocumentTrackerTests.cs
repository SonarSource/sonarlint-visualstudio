/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ActiveDocumentTrackerTests
    {
        private static readonly IVsWindowFrame ValidWindowFrame = Mock.Of<IVsWindowFrame>();

        private Mock<IVsMonitorSelection> monitorSelectionMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<Action<ActiveDocumentChangedEventArgs>> mockEventHandler;
        private Mock<ITextDocumentProvider> textDocumentProviderMock;
        private ITextDocument textDocumentMock;

        private ActiveDocumentTracker testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            textDocumentMock = Mock.Of<ITextDocument>();

            monitorSelectionMock = new Mock<IVsMonitorSelection>();

            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelectionMock.Object);

            textDocumentProviderMock = new Mock<ITextDocumentProvider>();
            mockEventHandler = new Mock<Action<ActiveDocumentChangedEventArgs>>();

            testSubject = new ActiveDocumentTracker(serviceProviderMock.Object, textDocumentProviderMock.Object);
            testSubject.ActiveDocumentChanged += (sender, e) => mockEventHandler.Object(e);
        }

        [TestMethod]
        public void Ctor_RegisterToSelectionEvents()
        {
            uint cookie = 1234;
            // Reset invocations that were performed in TestInitialize
            monitorSelectionMock.Reset();
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            testSubject = new ActiveDocumentTracker(serviceProviderMock.Object, textDocumentProviderMock.Object);

            monitorSelectionMock.Verify(x=> x.AdviseSelectionEvents(testSubject, out cookie), Times.Once);
            monitorSelectionMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_ShouldUnregisterFromSelectionEvents()
        {
            uint cookie = 1234;
            monitorSelectionMock.Setup(x => x.AdviseSelectionEvents(It.IsAny<IVsSelectionEvents>(), out cookie));

            testSubject = new ActiveDocumentTracker(serviceProviderMock.Object, textDocumentProviderMock.Object);
            testSubject.Dispose();

            monitorSelectionMock.Verify(x => x.UnadviseSelectionEvents(cookie), Times.Once);
        }

        [TestMethod]
        public void Dispose_ShouldNoLongerRaiseEvents()
        {
            var selectedFrame = ValidWindowFrame;

            testSubject.Dispose();
            (testSubject as IVsSelectionEvents).OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame);

            CheckEventNotRaised();
        }

        [TestMethod]
        public void OnCmdUIContextChanged_DoesNotRaiseEvent()
        {
            var result = (testSubject as IVsSelectionEvents).OnCmdUIContextChanged(1, 1);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised();
        }

        [TestMethod]
        public void OnSelectionChanged_DoesNotRaiseEvent()
        {
            var result = (testSubject as IVsSelectionEvents).OnSelectionChanged(null, 1, null, null, null, 1, null, null);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised();
        }

        [TestMethod]
        [DataRow(VSConstants.VSSELELEMID.SEID_PropertyBrowserSID)]
        [DataRow(VSConstants.VSSELELEMID.SEID_ResultList)]
        [DataRow(VSConstants.VSSELELEMID.SEID_StartupProject)]
        [DataRow(VSConstants.VSSELELEMID.SEID_UndoManager)]
        [DataRow(VSConstants.VSSELELEMID.SEID_UserContext)]
        [DataRow(VSConstants.VSSELELEMID.SEID_WindowFrame)]
        public void OnElementValueChanged_NonDocumentElementId_DoesNotRaiseEvent(VSConstants.VSSELELEMID elementId)
        {
            var selectedFrame = Mock.Of<IVsWindowFrame>();

            var result = SimulateElementValueChanged(elementId, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            mockEventHandler.VerifyNoOtherCalls();
            textDocumentProviderMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnElementValueChanged_DocumentElement_NewValueIsNull_RaisesEventWithNullArg()
        {
            var result = SimulateElementValueChanged(VSConstants.VSSELELEMID.SEID_DocumentFrame, null);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(null);
        }

        [TestMethod]
        public void OnElementValueChanged_DocumentElement_NullTextDocument_RaisesEventWithNullArg()
        {
            var selectedFrame = ValidWindowFrame;

            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(null as ITextDocument);

            var result = SimulateElementValueChanged(VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(null);
        }

        [TestMethod]
        public void OnElementValueChanged_DocumentElement_ValidTextDocument_RaisesEvent()
        {
            var selectedFrame = ValidWindowFrame;

            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(textDocumentMock);

            var result = SimulateElementValueChanged(VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(textDocumentMock);
        }

        [TestMethod]
        public void OnElementValueChanged_ValidDocumentFrame_NoEventSubscribers_DoesNotFail()
        {
            testSubject = new ActiveDocumentTracker(serviceProviderMock.Object, textDocumentProviderMock.Object);

            var selectedFrame = ValidWindowFrame;

            textDocumentProviderMock.Setup(x => x.GetFromFrame(selectedFrame)).Returns(textDocumentMock);

            Func<int> act = () => (testSubject as IVsSelectionEvents).OnElementValueChanged((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, "1", selectedFrame);

            act.Should().NotThrow().And.Subject().Should().Be(VSConstants.S_OK);
        }

        private int SimulateElementValueChanged(VSConstants.VSSELELEMID elementId, object newValue) =>
            (testSubject as IVsSelectionEvents).OnElementValueChanged((uint)elementId,
                "old value - can be anything",
                newValue);

        private void CheckEventRaised(ITextDocument expected) =>
            mockEventHandler.Verify(x => x(It.Is((ActiveDocumentChangedEventArgs e) => e.ActiveTextDocument == expected)), Times.Once);

        private void CheckEventNotRaised() =>
            mockEventHandler.Verify(x => x(It.IsAny<ActiveDocumentChangedEventArgs>()), Times.Never);
    }
}
