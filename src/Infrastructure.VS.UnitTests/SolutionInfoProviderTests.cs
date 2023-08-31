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
using Microsoft.VisualStudio;
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
    public class SolutionInfoProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SolutionInfoProvider, ISolutionInfoProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void CheckIsSharedMefComponent()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SolutionInfoProvider>();
        }

        [TestMethod]
        public void Ctor_DoesNotCallServices()
        {
            // The MEF constructor needs to be free-threaded, so it shouldn't switch threads or
            // call any components that switch threads.
            // In this case, we're not expecting it to call anything.

            var serviceProvider = new Mock<IServiceProvider>();
            _ = CreateTestSubject(serviceProvider.Object);

            serviceProvider.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("a value")]
        public async Task GetSlnFilePathAsync_ReturnsExpectedValue(string solutionNameToReturn)
        {
            var solution = CreateIVsSolutionWithSolutionFileName(solutionNameToReturn);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = await testSubject.GetFullSolutionFilePathAsync();
            actual.Should().Be(solutionNameToReturn);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("a value")]
        public void GetSlnFilePath_ReturnsExpectedValue(string solutionNameToReturn)
        {
            var solution = CreateIVsSolutionWithSolutionFileName(solutionNameToReturn);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.GetFullSolutionFilePath();
            actual.Should().Be(solutionNameToReturn);
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("c:\\aaa\\bbb\\mysolution.sln", "c:\\aaa\\bbb")]
        public async Task GetDirectoryAsync_ReturnsExpectedValue(string solutionNameToReturn, string expectedResult)
        {
            var solution = CreateIVsSolutionWithSolutionFileName(solutionNameToReturn);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = await testSubject.GetSolutionDirectoryAsync();
            actual.Should().Be(expectedResult);
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("c:\\aaa\\bbb\\mysolution.sln", "c:\\aaa\\bbb")]
        public void GetDirectory_ReturnsExpectedValue(string solutionNameToReturn, string expectedResult)
        {
            var solution = CreateIVsSolutionWithSolutionFileName(solutionNameToReturn);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.GetSolutionDirectory();
            actual.Should().Be(expectedResult);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task GetSolutionFullyOpenAsync_ReturnsExpectedValue(bool isFullyOpen)
        {
            var solution = CreateIVsSolutionWithIsFullyOpened(isFullyOpen);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = await testSubject.IsSolutionFullyOpenedAsync();
            actual.Should().Be(isFullyOpen);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSolutionFullyOpen_ReturnsExpectedValue(bool isFullyOpen)
        {
            var solution = CreateIVsSolutionWithIsFullyOpened(isFullyOpen);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.IsSolutionFullyOpened();
            actual.Should().Be(isFullyOpen);
        }

        [TestMethod]
        [DataRow(true, 0)]
        [DataRow(false, 0)]
        [DataRow(true, -1)]
        [DataRow(false, -1)]
        public void GetSolutionFullyOpen_HrefResult_ChangesOutcome(bool isFullyOpen, int hresult)
        {
            var solution = CreateIVsSolutionWithIsFullyOpened(isFullyOpen, hresult);
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object);

            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.IsSolutionFullyOpened();
            actual.Should().Be(hresult == 0 && isFullyOpen);
        }

        [TestMethod]
        public async Task GetSlnFilePathAsync_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithSolutionFileName("any", () => calls.Add("GetSolutionName"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });


            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = await testSubject.GetFullSolutionFilePathAsync();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionName");
        }

        [TestMethod]
        public async Task GetDirectoryAsync_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithSolutionFileName("any", () => calls.Add("GetSolutionName"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = await testSubject.GetSolutionDirectoryAsync();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionName");
        }

        [TestMethod]
        public async Task IsSolutionFullyOpenAsync_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithIsFullyOpened(true, callback:() => calls.Add("GetSolutionIsFullyOpen"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = await testSubject.IsSolutionFullyOpenedAsync();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionIsFullyOpen");
        }

        [TestMethod]
        public void GetSlnFilePath_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithSolutionFileName("any", () => calls.Add("GetSolutionName"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = testSubject.GetFullSolutionFilePath();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionName");
        }

        [TestMethod]
        public void GetDirectory_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithSolutionFileName("any", () => calls.Add("GetSolutionName"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = testSubject.GetSolutionDirectory();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionName");
        }

        [TestMethod]
        public async Task IsSolutionFullyOpen_ServiceCalledOnUIThread()
        {
            var calls = new List<string>();

            var solution = CreateIVsSolutionWithIsFullyOpened(true, callback: () => calls.Add("GetSolutionIsFullyOpen"));
            var serviceProvider = CreateServiceProviderWithSolution(solution.Object, () => calls.Add("GetService"));

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    calls.Add("switch to UI thread");
                    productOperation.Invoke();
                });

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling.Object);

            var actual = await testSubject.IsSolutionFullyOpenedAsync();

            calls.Should().ContainInOrder("switch to UI thread", "GetService", "GetSolutionIsFullyOpen");
        }

        private static SolutionInfoProvider CreateTestSubject(IServiceProvider serviceProvider,
            IThreadHandling threadHandling = null)
        {
            serviceProvider ??= Mock.Of<IServiceProvider>();
            threadHandling ??= new NoOpThreadHandler();

            return new SolutionInfoProvider(serviceProvider, threadHandling);
        }

        private static Mock<IServiceProvider> CreateServiceProviderWithSolution(IVsSolution solution, Action callback = null)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution)))
                .Callback<IInvocation>(x => callback?.Invoke())
                .Returns(solution);

            return serviceProvider;
        }

        private static Mock<IVsSolution> CreateIVsSolutionWithSolutionFileName(string solutionNameToReturn, Action callback = null)
        {
            var solution = new Mock<IVsSolution>();

            object solutionDirectory = solutionNameToReturn;
            solution
                .Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out solutionDirectory))
                .Callback<IInvocation>(x => callback?.Invoke())
                .Returns(VSConstants.S_OK);

            return solution;
        }

        private static Mock<IVsSolution> CreateIVsSolutionWithIsFullyOpened(bool isFullyOpened, int hresult = VSConstants.S_OK,Action callback = null)
        {
            var solution = new Mock<IVsSolution>();

            object isOpened = isFullyOpened;
            solution
                .Setup(x => x.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out isOpened))
                .Callback<IInvocation>(x => callback?.Invoke())
                .Returns(hresult);

            return solution;
        }

        private static Mock<IThreadHandling> CreateThreadHandlingWithRunOnUICallback(Action testOperation)
        {
            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback<Action>(productOperation =>
                {
                    testOperation();
                    productOperation.Invoke();
                });
            return threadHandling;
        }
    }
}
