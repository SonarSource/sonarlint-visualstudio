/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ActiveDocumentTrackerTests
    {
        private IVsMonitorSelection monitorSelection;
        private ITextDocumentProvider textDocumentProvider;
        private IServiceProvider serviceProvider;
        private IInitializationProcessorFactory initializationProcessorFactory;
        private IThreadHandling threadHandling;
        private ILogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            monitorSelection ??= Substitute.For<IVsMonitorSelection>();
            textDocumentProvider ??= Substitute.For<ITextDocumentProvider>();

            serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider
                .GetService(typeof(SVsShellMonitorSelection))
                .Returns(monitorSelection);

            threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
            logger = Substitute.ForPartsOf<TestLogger>();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ActiveDocumentTracker, IActiveDocumentTracker>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ITextDocumentProvider>(),
                MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
                MefTestHelpers.CreateExport<IThreadHandling>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() =>
            MefTestHelpers.CheckIsSingletonMefComponent<ActiveDocumentTracker>();

        [TestMethod]
        public void Ctor_RegisterToSelectionEvents()
        {
            var cookie = SetUpCookie();

            var testSubject = CreateAndInitializeTestSubject();

            Received.InOrder(() =>
            {
                initializationProcessorFactory.Create<ActiveDocumentTracker>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
                testSubject.InitializationProcessor.InitializeAsync();
                threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
                serviceProvider.GetService(typeof(SVsShellMonitorSelection));
                monitorSelection.AdviseSelectionEvents(testSubject, out _);
                testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
            });
        }

        [TestMethod]
        public void Ctor_DoesNotSubscribeToEventsBeforeInitialization()
        {
            var testSubject = CreateUninitializedTestSubject(out var barrier);

            serviceProvider.DidNotReceiveWithAnyArgs().GetService(default);
            monitorSelection.DidNotReceiveWithAnyArgs().AdviseSelectionEvents(default, out _);

            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            serviceProvider.Received(1).GetService(typeof(SVsShellMonitorSelection));
            monitorSelection.Received(1).AdviseSelectionEvents(testSubject, out _);
        }


        [TestMethod]
        public void Initialization_DoesNotSubscribeToEventsIfAlreadyDisposed()
        {
            var testSubject = CreateUninitializedTestSubject(out var barrier);

            serviceProvider.DidNotReceiveWithAnyArgs().GetService(default);
            monitorSelection.DidNotReceiveWithAnyArgs().AdviseSelectionEvents(default, out _);

            testSubject.Dispose();
            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            serviceProvider.DidNotReceiveWithAnyArgs().GetService(default);
            monitorSelection.DidNotReceiveWithAnyArgs().AdviseSelectionEvents(default, out _);
            monitorSelection.DidNotReceiveWithAnyArgs().UnadviseSelectionEvents(default);
        }

        [TestMethod]
        public void Dispose_ShouldUnregisterFromSelectionEvents()
        {
            var cookie = SetUpCookie();
            var testSubject = CreateAndInitializeTestSubject();

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            monitorSelection.Received(1).UnadviseSelectionEvents(cookie);
        }

        [TestMethod]
        [DataRow(VSConstants.VSSELELEMID.SEID_DocumentFrame)]
        [DataRow(VSConstants.VSSELELEMID.SEID_WindowFrame)]
        public void Dispose_ShouldNoLongerRaiseEvents(VSConstants.VSSELELEMID elementId)
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);
            var textDocument = Substitute.For<ITextDocument>();
            textDocumentProvider.GetFromFrame(selectedFrame).Returns(textDocument);

            SimulateElementValueChanged(testSubject, elementId, selectedFrame);

            CheckEventRaised(eventHandler, textDocument);

            eventHandler.ClearReceivedCalls();
            testSubject.Dispose();

            SimulateElementValueChanged(testSubject, elementId, selectedFrame);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnCmdUIContextChanged_DoesNotRaiseEvent()
        {
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = (testSubject as IVsSelectionEvents).OnCmdUIContextChanged(1, 1);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnSelectionChanged_DoesNotRaiseEvent()
        {
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

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
            var selectedFrame = Substitute.For<IVsWindowFrame>();
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = SimulateElementValueChanged(testSubject, elementId, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("not a frame")]
        public void OnElementValueChanged_DocumentElement_NewValueIsNotAFrame_RaisesEventWithNullArg(object newValue)
        {
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, null);
        }

        [TestMethod, Description("Regression test for #2091")]
        public void OnElementValueChanged_DocumentElement_NullTextDocument_RaisesEventWithNullArg()
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            textDocumentProvider.GetFromFrame(selectedFrame).Returns(null as ITextDocument);
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, null);
        }

        [TestMethod]
        public void OnElementValueChanged_DocumentElement_ValidTextDocument_RaisesEvent()
        {
            var selectedFrame = CreateWindowFrame(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Document);
            var textDocumentMock = Substitute.For<ITextDocument>();
            textDocumentProvider.GetFromFrame(selectedFrame).Returns(textDocumentMock);
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_DocumentFrame, selectedFrame);
            result.Should().Be(VSConstants.S_OK);

            CheckEventRaised(eventHandler, textDocumentMock);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("not a frame")]
        public void OnElementValueChanged_FrameElement_NewValueIsNotAFrame_DoesNotRaiseEvent(object newValue)
        {
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

            var result = SimulateElementValueChanged(testSubject, VSConstants.VSSELELEMID.SEID_WindowFrame, newValue);
            result.Should().Be(VSConstants.S_OK);

            CheckEventNotRaised(eventHandler);
        }

        [TestMethod]
        public void OnElementValueChanged_FrameElement_NewValueIsNotDocumentFrame_DoesNotRaiseEvent()
        {
            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);
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

            var textDocumentMock = isNullDocument ? null : Substitute.For<ITextDocument>();
            textDocumentProvider.GetFromFrame(newValue).Returns(textDocumentMock);

            var testSubject = CreateTestSubjectAndSubscribe(out var eventHandler);

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

            var textDocumentMock = Substitute.For<ITextDocument>();
            textDocumentProvider.GetFromFrame(selectedFrame).Returns(textDocumentMock);

            var testSubject = CreateAndInitializeTestSubject();

            Func<int> act = () => (testSubject as IVsSelectionEvents).OnElementValueChanged((uint) elementId, "1", selectedFrame);

            act.Should().NotThrow().And.Subject().Should().Be(VSConstants.S_OK);
        }

        private ActiveDocumentTracker CreateTestSubjectAndSubscribe(out EventHandler<ActiveDocumentChangedEventArgs> eventHandler)
        {
            eventHandler = Substitute.For<EventHandler<ActiveDocumentChangedEventArgs>>();
            var testSubject = CreateAndInitializeTestSubject();
            testSubject.ActiveDocumentChanged += eventHandler;
            return testSubject;
        }

        private ActiveDocumentTracker CreateAndInitializeTestSubject()
        {
            initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveDocumentTracker>(threadHandling, logger);
            var testSubject = new ActiveDocumentTracker(serviceProvider, textDocumentProvider, initializationProcessorFactory, threadHandling);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
            return testSubject;
        }

        private ActiveDocumentTracker CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
        {
            var tcs = barrier = new TaskCompletionSource<byte>();
            initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveDocumentTracker>(threadHandling, logger, processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));
            return new ActiveDocumentTracker(serviceProvider, textDocumentProvider, initializationProcessorFactory, threadHandling);
        }

        private uint SetUpCookie()
        {
            uint cookie = 1234;
            monitorSelection.AdviseSelectionEvents(Arg.Any<IVsSelectionEvents>(), out Arg.Any<uint>()).Returns(info =>
            {
                info[1] = cookie;
                return VSConstants.S_OK;
            });
            return cookie;
        }

        private static IVsWindowFrame CreateWindowFrame(__WindowFrameTypeFlags frameType)
        {
            object frameTypeObj = (int)frameType;

            var frame = Substitute.For<IVsWindowFrame>();
            frame
                .GetProperty((int)__VSFPROPID.VSFPROPID_Type, out Arg.Any<object>())
                .Returns(info =>
                {
                    info[1] = frameTypeObj;
                    return VSConstants.S_OK;
                });

            return frame;
        }

        private int SimulateElementValueChanged(IVsSelectionEvents testSubject, VSConstants.VSSELELEMID elementId, object newValue)
        {
            var result = testSubject.OnElementValueChanged((uint)elementId,
                "old value - can be anything",
                newValue);
            threadHandling.Received(1).ThrowIfNotOnUIThread();
            threadHandling.ClearReceivedCalls();
            return result;
        }

        private static void CheckEventRaised(EventHandler<ActiveDocumentChangedEventArgs> mockEventHandler, ITextDocument expected) =>
            mockEventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<ActiveDocumentChangedEventArgs>(e =>
                e.ActiveTextDocument == expected));

        private static void CheckEventNotRaised(EventHandler<ActiveDocumentChangedEventArgs> mockEventHandler) =>
            mockEventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }
}
