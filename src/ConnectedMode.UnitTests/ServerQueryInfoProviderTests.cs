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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class ServerQueryInfoProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerQueryInfoProvider, IServerQueryInfoProvider>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>());
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task Get_IsConnectedMode_ReturnsExpectedValues(SonarLintMode mode)
        {
            // Happy path - in connected mode, has branch
            var config = CreateBindingConfig(mode, "my project key");
            var configProvider = CreateConfigProvider(config);

            var branchProvider = CreateBranchProvider("my branch");

            var testSubject = CreateTestSubject(configProvider.Object, branchProvider.Object);

            var actual = await testSubject.GetProjectKeyAndBranchAsync(CancellationToken.None);

            actual.projectKey.Should().Be("my project key");
            actual.branchName.Should().Be("my branch");
        }

        [TestMethod]
        public async Task Get_IsStandaloneMode_ReturnsNulls()
        {
            var config = CreateBindingConfig(SonarLintMode.Standalone, "my project key");
            var configProvider = CreateConfigProvider(config);

            var testSubject = CreateTestSubject(configProvider.Object);

            var actual = await testSubject.GetProjectKeyAndBranchAsync(CancellationToken.None);

            actual.projectKey.Should().BeNull();
            actual.branchName.Should().BeNull();
        }

        [TestMethod]
        public void Get_OperationIsCancelled_ThrowsOperationCancelledException()
        {
            // Tests that the class doesn't squash OperationCancelledExceptions
            // i.e. they are the callers responsibility

            var config = CreateBindingConfig(SonarLintMode.Connected, "my project key");
            var configProvider = CreateConfigProvider(config);

            var cancellationTokenSource = new CancellationTokenSource();

            var branchProvider = new Mock<IStatefulServerBranchProvider>();
            branchProvider.Setup(x => x.GetServerBranchNameAsync(cancellationTokenSource.Token)).
                Callback(() =>
                {
                    // Simulate what happens when the cancellation token is cancelled
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                });

            var testSubject = CreateTestSubject(configProvider.Object, branchProvider.Object);

            Func<Task> operation = () => testSubject.GetProjectKeyAndBranchAsync(cancellationTokenSource.Token);

            operation.Should().Throw<OperationCanceledException>();
        }


        private static ServerQueryInfoProvider CreateTestSubject(IConfigurationProvider configurationProvider = null,
            IStatefulServerBranchProvider serverBranchProvider = null)
        {
            configurationProvider ??= Mock.Of<IConfigurationProvider>();
            serverBranchProvider ??= Mock.Of<IStatefulServerBranchProvider>();

            var testSubject = new ServerQueryInfoProvider(configurationProvider, serverBranchProvider);
            return testSubject;
        }

        private static BindingConfiguration CreateBindingConfig(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any")
            => new(new BoundSonarQubeProject { ProjectKey = projectKey }, mode, "any dir");

        private static Mock<IConfigurationProvider> CreateConfigProvider(BindingConfiguration config = null)
        {
            config ??= CreateBindingConfig();

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(config);
            return configProvider;
        }

        private static Mock<IStatefulServerBranchProvider> CreateBranchProvider(string branchName)
            => CreateBranchProvider(branchName, CancellationToken.None);

        private static Mock<IStatefulServerBranchProvider> CreateBranchProvider(string branchName,
            CancellationToken cancellationToken)
        {
            var mock = new Mock<IStatefulServerBranchProvider>();
            mock.Setup(x => x.GetServerBranchNameAsync(cancellationToken)).ReturnsAsync(branchName);
            return mock;
        }
    }
}
