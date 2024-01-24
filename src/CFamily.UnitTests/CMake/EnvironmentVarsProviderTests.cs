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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class EnvironmentVarsProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<EnvironmentVarsProvider, IEnvironmentVarsProvider>(
                MefTestHelpers.CreateExport<IVsDevCmdEnvironmentProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task TryGet_Caching()
        {
            var envVars_AAA = new Dictionary<string, string> { { "AAA", "111" } };
            var envVars_BBB = new Dictionary<string, string> { { "BBB", "222" } };

            var vsDevCmdProvider = new Mock<IVsDevCmdEnvironmentProvider>()
                .SetEnvVars("AAA", envVars_AAA)
                .SetEnvVars("BBB", envVars_BBB);

            var testSubject = CreateTestSubject(vsDevCmdProvider.Object);

            // 1. ScriptParams AAA: empty cache => cache miss => provider called
            var case1 = await testSubject.GetAsync("AAA");

            case1.Should().BeSameAs(envVars_AAA);
            vsDevCmdProvider.CheckSettingsFetchedOnce("AAA");
            vsDevCmdProvider.CheckSettingsNotFetched("BBB");

            // 2. ScriptParamsA: cache hit => provider not called
            var case2 = await testSubject.GetAsync("AAA");

            case2.Should().BeSameAs(envVars_AAA);
            vsDevCmdProvider.CheckSettingsFetchedOnce("AAA");
            vsDevCmdProvider.CheckSettingsNotFetched("BBB");

            // 3. ScriptParamsB: different params, cache miss => provider called
            var case3 = await testSubject.GetAsync("BBB");

            case3.Should().BeSameAs(envVars_BBB);
            vsDevCmdProvider.CheckSettingsFetchedOnce("AAA");
            vsDevCmdProvider.CheckSettingsFetchedOnce("BBB");
        }

        [TestMethod]
        public async Task TryGet_NullVsDevCmdResultsAreCached()
        {
            var vsDevCmdProvider = new Mock<IVsDevCmdEnvironmentProvider>()
                .SetEnvVars(string.Empty, null);

            var testSubject = CreateTestSubject(vsDevCmdProvider.Object);

            // 1. empty cache => cache miss => provider called
            var actual = await testSubject.GetAsync(string.Empty);

            actual.Should().BeNull();
            vsDevCmdProvider.CheckSettingsFetchedOnce(string.Empty);

            // 2. Call again - null result should have been cached i.e. we don't
            // retry calling just because we didn't get a non-null result first time.
            actual = await testSubject.GetAsync(string.Empty);

            actual.Should().BeNull();
            vsDevCmdProvider.CheckSettingsFetchedOnce(string.Empty);
        }

        [TestMethod]
        public void TryGet_SecondCallIsBlockedUntilFirstCallCompletes()
        {
            const int timeoutMs = 300;

            var firstCallStarted = new ManualResetEvent(false);
            var allowFirstCallToComplete = new ManualResetEvent(false);

            void FirstCallToProviderOp()
            {
                // Let the test know that the provider has been called i.e. we're inside the lock
                firstCallStarted.Set();

                // Wait until the test indicates that it wants the call to finish i.e. to release the lock
                allowFirstCallToComplete.WaitOne();
            }

            // Two calls with same script params, so the provider should only be called once
            // and return the same result
            var provider = new Mock<IVsDevCmdEnvironmentProvider>();
            provider.Setup(x => x.GetAsync(string.Empty))
                .Callback(FirstCallToProviderOp)
                .ReturnsAsync(new Dictionary<string, string>());

            var testSubject = CreateTestSubject(provider.Object);

            // Start first call and wait for it to report it has started
            var firstCall = Task.Run(() => testSubject.GetAsync());
            var firstTaskRunning = firstCallStarted.WaitOne(timeoutMs);
            firstTaskRunning.Should().BeTrue();
            provider.Invocations.Count.Should().Be(1);

            // Start second call - should be blocked by lock
            var secondCall = Task.Run(() => testSubject.GetAsync());
            var secondCallFinished = secondCall.Wait(500);
            secondCallFinished.Should().BeFalse();
            
            // Check the second call thread is waiting for the lock
            secondCall.Status.Should().Be(TaskStatus.WaitingForActivation);

            // Unblock the first call
            allowFirstCallToComplete.Set();
            var firstCallCompleted = firstCall.Wait(timeoutMs);
            firstCallCompleted.Should().BeTrue();

            var secondCallCompleted = secondCall.Wait(timeoutMs);
            secondCallCompleted.Should().BeTrue();

            secondCall.Result.Should().BeSameAs(firstCall.Result);
            provider.Invocations.Count.Should().Be(1);
        }

        private static EnvironmentVarsProvider CreateTestSubject(IVsDevCmdEnvironmentProvider vsDevCmdProvider)
        {
            var testSubject = new EnvironmentVarsProvider(vsDevCmdProvider, new TestLogger(logToConsole: true));
            return testSubject;
        }
    }

    internal static class EnvironmentVarsProviderTestsExtensions
    {
        public static Mock<IVsDevCmdEnvironmentProvider> SetEnvVars(this Mock<IVsDevCmdEnvironmentProvider> provider, string scriptParams, IReadOnlyDictionary<string, string> envVars)
        {
            provider.Setup(x => x.GetAsync(scriptParams)).Returns(Task.FromResult(envVars));
            return provider;
        }

        public static void CheckSettingsFetchedOnce(this Mock<IVsDevCmdEnvironmentProvider> provider, string scriptParms) =>
            provider.Verify(x => x.GetAsync(scriptParms), Times.Once);

        public static void CheckSettingsNotFetched(this Mock<IVsDevCmdEnvironmentProvider> provider, string scriptParms) =>
            provider.Verify(x => x.GetAsync(scriptParms), Times.Never);
    }
}
