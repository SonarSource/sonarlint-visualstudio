/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class VSServiceOperationTests
    {
        // Dummy interfaces used in the tests
#pragma warning disable IDE1006 // Naming Styles
        public interface SMyInterface { }
#pragma warning restore IDE1006 // Naming Styles
        public interface IMyInterface { }

        #region MEF tests

        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<VsServiceOperation, IVSServiceOperation>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IThreadHandling>());

        [TestMethod]
        public void CheckIsNonSharedMefComponent()
            => MefTestHelpers.CheckIsNonSharedMefComponent<VsServiceOperation>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var threadHandling = new Mock<IThreadHandling>();

            _ = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            serviceProvider.Invocations.Should().BeEmpty();
            threadHandling.Invocations.Should().BeEmpty();
        }

        #endregion

        #region RunOnUIThread tests

        [TestMethod]
        public void Execute_Action_RunOnUIThread()
        {
            (var calls, var testSubject) = SetupUpThreadHandlingTests<SMyInterface>();

            var callback = (IMyInterface x) => calls.Add("callback");

            // Act
            testSubject.Execute<SMyInterface, IMyInterface>(callback);

            calls.Should().ContainInOrder(
                "change thread - sync",
                "get service",
                "callback");
        }

        [TestMethod]
        public void Execute_Func_RunOnUIThread()
        {
            (var calls, var testSubject) = SetupUpThreadHandlingTests<SMyInterface>();

            string callback(IMyInterface x)
            {
                calls.Add("callback");
                return "anything";
            }

            // Act
            _ = testSubject.Execute<SMyInterface, IMyInterface, string>(callback);

            calls.Should().ContainInOrder(
                "change thread - sync",
                "get service",
                "callback");
        }

        [TestMethod]
        public async Task ExecuteAsync_Action_RunOnUIThread()
        {
            (var calls, var testSubject) = SetupUpThreadHandlingTests<SMyInterface>();

            var callback = (IMyInterface x) => calls.Add("callback");

            // Act
            await testSubject.ExecuteAsync<SMyInterface, IMyInterface>(callback);

            calls.Should().ContainInOrder(
                "change thread - async",
                "get service",
                "callback");
        }

        [TestMethod]
        public async Task ExecuteAsync_Func_RunOnUIThread()
        {
            (var calls, var testSubject) = SetupUpThreadHandlingTests<SMyInterface>();

            string callback(IMyInterface x)
            {
                calls.Add("callback");
                return "anything";
            }

            // Act
            _ = await testSubject.ExecuteAsync<SMyInterface, IMyInterface, string>(callback);

            calls.Should().ContainInOrder(
                "change thread - async",
                "get service",
                "callback");
        }

        private static (IList<string> calls, VsServiceOperation testSubject) SetupUpThreadHandlingTests<S>()
        {
            var calls = new List<string>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(S)))
                .Callback(() => calls.Add("get service"));

            // The method does the setup for the sync and async tests, so we'll set up both
            // RunOnUIThread and RunOnUIThreadAsync
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(x =>
                {
                    calls.Add("change thread - sync");
                    x();
                });

            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(x =>
                {
                    calls.Add("change thread - async");
                    x();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            return (calls, testSubject);
        }

        #endregion

        #region Func methods return results

        [TestMethod]
        public void Execute_Func_ReturnsValueFromCallback()
        {
            (var callback, var testSubject) = SetupUpFuncReturnsValueTests("expected sync result");

            // Act
            var result = testSubject.Execute<SMyInterface, IMyInterface, string>(callback.Object);

            result.Should().Be("expected sync result");
            callback.Verify(x => x.Invoke(It.IsAny<IMyInterface>()), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteAsync_Func_ReturnsValueFromCallback()
        {
            (var callback, var testSubject) = SetupUpFuncReturnsValueTests("expected async result");

            // Act
            var result = await testSubject.ExecuteAsync<SMyInterface, IMyInterface, string>(callback.Object);

            result.Should().Be("expected async result");
            callback.Verify(x => x.Invoke(It.IsAny<IMyInterface>()), Times.Once);
        }

        private static (Mock<Func<IMyInterface, string>> callback, VsServiceOperation vsServiceOperation) SetupUpFuncReturnsValueTests(string callbackResultToReturn)
        {
            var service = Mock.Of<IMyInterface>();
            var serviceProvider = CreateConfiguredServiceProvider<SMyInterface>(service);
            var callback = new Mock<Func<IMyInterface, string>>();
            callback.Setup(x => x.Invoke(service)).Returns(callbackResultToReturn);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            return (callback, testSubject);
        }

        #endregion

        #region Multiple calls, same service tests

        [TestMethod]
        public void Execute_Action_MultipleCalls_SameService_ServiceProviderIsCalledOnlyOnce()
        {
            var uiShellService = Mock.Of<IVsUIShell>();
            var serviceProvider = CreateConfiguredServiceProvider<SVsUIShell>(uiShellService);
            var callback = new Mock<Action<IVsUIShell>>();

            var testSubject = CreateTestSubject(serviceProvider.Object);

            // 1. First call for IVsUIShell -> calls the service provider
            testSubject.Execute<SVsUIShell, IVsUIShell>(callback.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsUIShell)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(1);
            callback.Verify(x => x.Invoke(uiShellService), Times.Once);

            // 2. Second call for the same service -> does not call the service provider
            testSubject.Execute<SVsUIShell, IVsUIShell>(callback.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsUIShell)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(1);
            callback.Verify(x => x.Invoke(uiShellService), Times.Exactly(2)); // called again with the same service instance
        }

        [TestMethod]
        public void Execute_Func_MultipleCalls_SameService_ServiceProviderIsCalledOnlyOnce()
        {
            var uiShellService = Mock.Of<IVsUIShell>();
            var serviceProvider = CreateConfiguredServiceProvider<SVsUIShell>(uiShellService);
            var callback = new Mock<Func<IVsUIShell, string>>();

            var testSubject = CreateTestSubject(serviceProvider.Object);

            // 1. First call for IVsUIShell -> calls the service provider
            _ = testSubject.Execute<SVsUIShell, IVsUIShell, string>(callback.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsUIShell)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(1);
            callback.Verify(x => x.Invoke(uiShellService), Times.Once);

            // 2. Second call for the same service -> does not call the service provider
            _ = testSubject.Execute<SVsUIShell, IVsUIShell, string>(callback.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsUIShell)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(1);
            callback.Verify(x => x.Invoke(uiShellService), Times.Exactly(2)); // called again with the same service instance
        }

        #endregion

        #region Multiple calls, same different service

        [TestMethod]
        public void Execute_Action_MultipleCalls_DifferentServices_ServiceProviderIsCalledForEachService()
        {
            var service1 = Mock.Of<IVsUIShell>();
            var service2 = Mock.Of<IVsMonitorSelection>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsUIShell))).Returns(service1);
            serviceProvider.Setup(x => x.GetService(typeof(SVsShellMonitorSelection))).Returns(service2);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            // 1. First call for IVsUIShell -> calls the service provider
            var callback1 = new Mock<Action<IVsUIShell>>();
            testSubject.Execute<SVsUIShell, IVsUIShell>(callback1.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsUIShell)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(1);
            callback1.Verify(x => x.Invoke(service1), Times.Once);

            // 2. Second call for the same service -> does not call the service provider
            var callback2 = new Mock<Action<IVsMonitorSelection>>();
            testSubject.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(callback2.Object);

            serviceProvider.Verify(x => x.GetService(typeof(SVsShellMonitorSelection)), Times.Once);
            serviceProvider.Invocations.Should().HaveCount(2);
            callback2.Verify(x => x.Invoke(service2), Times.Once);
        }

        #endregion

        private static Mock<IServiceProvider> CreateConfiguredServiceProvider<S>(object serviceToReturn) 
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(S))).Returns(serviceToReturn);
            return serviceProvider;
        }

        private static VsServiceOperation CreateTestSubject(
            IServiceProvider serviceProvider,
            IThreadHandling threadHandling = null)
            => new(serviceProvider, threadHandling ?? new NoOpThreadHandler());
    }
}
