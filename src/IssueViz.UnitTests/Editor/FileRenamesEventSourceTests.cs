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
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class FileRenamesEventSourceTests
    {
        private IVsTrackProjectDocuments2 trackProjectDocuments;
        private IVsUIServiceOperation serviceOperation;
        private IInitializationProcessorFactory initializationProcessorFactory;
        private IThreadHandling threadHandling;
        private uint cookie;

        [TestInitialize]
        public void TestInitialize()
        {
            trackProjectDocuments = Substitute.For<IVsTrackProjectDocuments2>();
            serviceOperation = Substitute.For<IVsUIServiceOperation>();
            serviceOperation
                .When(x => x.ExecuteAsync<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(Arg.Any<Action<IVsTrackProjectDocuments2>>()))
                .Do(call =>
                {
                    var action = (Action<IVsTrackProjectDocuments2>)call.Args()[0];
                    action(trackProjectDocuments);
                });
            serviceOperation
                .When(x => x.Execute<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(Arg.Any<Action<IVsTrackProjectDocuments2>>()))
                .Do(call =>
                {
                    var action = (Action<IVsTrackProjectDocuments2>)call.Args()[0];
                    action(trackProjectDocuments);
                });
            threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<FileRenamesEventSource, IFileRenamesEventSource>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FileRenamesEventSource>();

        [TestMethod]
        public void Ctor_RegisterToAdviseTrackProjectDocumentsEvents_InOrder()
        {
            var testSubject = CreateAndInitializeTestSubject();

            Received.InOrder(() =>
            {
                initializationProcessorFactory.Create<FileRenamesEventSource>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
                testSubject.InitializationProcessor.InitializeAsync();
                serviceOperation.ExecuteAsync<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(Arg.Any<Action<IVsTrackProjectDocuments2>>());
                trackProjectDocuments.AdviseTrackProjectDocumentsEvents(testSubject, out _);
                testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
            });
        }

        [TestMethod]
        public void Dispose_UnregisterFromAdviseTrackProjectDocumentsEvents()
        {
            cookie = 1234;
            trackProjectDocuments.AdviseTrackProjectDocumentsEvents(Arg.Any<IVsTrackProjectDocumentsEvents2>(), out Arg.Any<uint>())
                .Returns(info =>
                {
                    info[1] = cookie;
                    return VSConstants.S_OK;
                });
            trackProjectDocuments.UnadviseTrackProjectDocumentsEvents(cookie).Returns(VSConstants.S_OK);

            var testSubject = CreateAndInitializeTestSubject();
            testSubject.Dispose();
            testSubject.Dispose();

            serviceOperation.Received(1).Execute<SVsTrackProjectDocuments, IVsTrackProjectDocuments2>(Arg.Any<Action<IVsTrackProjectDocuments2>>());
            trackProjectDocuments.Received(1).UnadviseTrackProjectDocumentsEvents(cookie);
        }

        [TestMethod]
        public void AfterDocumentsRenamed_NoSubscribers_NoException()
        {
            var testSubject = CreateAndInitializeTestSubject();
            Action act = () => SimulateDocumentsRenamed(testSubject, new Dictionary<string, string> { { "old name", "new name" } });
            act.Should().NotThrow();
        }

        [TestMethod]
        public void AfterDocumentsRenamed_HasSubscribers_RaisesEvent()
        {
            var testSubject = CreateAndInitializeTestSubject();
            var eventHandler = Substitute.For<EventHandler<FilesRenamedEventArgs>>();
            testSubject.FilesRenamed += eventHandler;

            var renamedFiles = new Dictionary<string, string> { { "old name1", "new name1" }, { "old name2", "new name2" } };

            SimulateDocumentsRenamed(testSubject, renamedFiles);

            eventHandler.Received(1).Invoke(testSubject, Arg.Is<FilesRenamedEventArgs>(args =>
                args.OldNewFilePaths.Count == renamedFiles.Count &&
                args.OldNewFilePaths.All(arg => renamedFiles.ContainsKey(arg.Key) && renamedFiles[arg.Key] == arg.Value)));
        }

        [TestMethod]
        public void IVsTrackProjectDocumentsEvents2_AllMethodsReturnOkStatus()
        {
            var testSubject = CreateAndInitializeTestSubject();
            var vsTrackProjectDocumentsEvents2 = testSubject as IVsTrackProjectDocumentsEvents2;

            var notImplementedMethods = typeof(IVsTrackProjectDocumentsEvents2)
                .GetMethods()
                .Where(x => !x.Name.Equals(nameof(IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles)));

            foreach (var notImplementedMethod in notImplementedMethods)
            {
                var defaultValues = notImplementedMethod.GetParameters().Select(x => GetDefaultValue(x.ParameterType));
                var result = notImplementedMethod.Invoke(vsTrackProjectDocumentsEvents2, defaultValues.ToArray());
                result.Should().Be(VSConstants.S_OK, $"Method {notImplementedMethod.Name} should return VSConstants.S_OK");
            }
        }

        [TestMethod]
        public void Initialization_DoesNotAdviseEventsBeforeInitialization()
        {
            var testSubject = CreateUninitializedTestSubject(out var barrier);

            trackProjectDocuments.DidNotReceiveWithAnyArgs().AdviseTrackProjectDocumentsEvents(default, out _);

            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            trackProjectDocuments.Received(1).AdviseTrackProjectDocumentsEvents(testSubject, out _);
        }

        [TestMethod]
        public void Initialization_DoesNotAdviseEventsIfAlreadyDisposed()
        {
            var testSubject = CreateUninitializedTestSubject(out var barrier);

            trackProjectDocuments.DidNotReceiveWithAnyArgs().AdviseTrackProjectDocumentsEvents(default, out _);

            testSubject.Dispose();
            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            trackProjectDocuments.DidNotReceiveWithAnyArgs().AdviseTrackProjectDocumentsEvents(default, out _);
            trackProjectDocuments.DidNotReceiveWithAnyArgs().UnadviseTrackProjectDocumentsEvents(default);
        }

        private FileRenamesEventSource CreateAndInitializeTestSubject()
        {
            initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<FileRenamesEventSource>(threadHandling, Substitute.For<ILogger>());
            var testSubject = new FileRenamesEventSource(initializationProcessorFactory, serviceOperation);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
            return testSubject;
        }

        private FileRenamesEventSource CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
        {
            var tcs = barrier = new TaskCompletionSource<byte>();
            initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<FileRenamesEventSource>(threadHandling, Substitute.For<ILogger>(),
                processor => MockableInitializationProcessor.ConfigureWithWait(processor, tcs));
            return new FileRenamesEventSource(initializationProcessorFactory, serviceOperation);
        }

        private static void SimulateDocumentsRenamed(FileRenamesEventSource testSubject, IDictionary<string, string> oldNewFilePaths)
        {
            (testSubject as IVsTrackProjectDocumentsEvents2).OnAfterRenameFiles(0, oldNewFilePaths.Count, null, new int[0],
                oldNewFilePaths.Keys.ToArray(), oldNewFilePaths.Values.ToArray(), null);
        }

        public static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
