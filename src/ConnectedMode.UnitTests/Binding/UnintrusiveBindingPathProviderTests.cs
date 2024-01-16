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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class UnintrusiveBindingPathProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingPathProvider, IUnintrusiveBindingPathProvider>(
                    MefTestHelpers.CreateExport<ISolutionInfoProvider>());

        [TestMethod]
        public void MefCtor_CheckIsNonSharedMefComponent()
            => MefTestHelpers.CheckIsNonSharedMefComponent<UnintrusiveBindingController>();

        [TestMethod]
        public void Ctor_DoesNotCallServices()
        {
            // The constructor should be free-threaded i.e. run entirely on the calling thread
            // -> should not call services that swtich threads
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();

            _ = CreateTestSubject(solutionInfoProvider.Object);

            solutionInfoProvider.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Get_NoOpenSolution_ReturnsNull()
        {
            var serviceProvider = CreateSolutionInfoProvider(null);
            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.Get();
            actual.Should().BeNull();
        }

        [TestMethod]
        public void Get_HasOpenSolution_ReturnsExpectedValue()
        {
            const string solutionName = "mysolutionName";
            const string rootFolderName = @"x:\users\foo\";

            var solutionInfoProvider = CreateSolutionInfoProvider(solutionName);
            var envVars = CreateEnvVars(rootFolderName);

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, envVars);

            var actual = testSubject.Get();

            actual.Should().Be($@"{rootFolderName}SonarLint for Visual Studio\Bindings\{solutionName}\binding.config");
        }

        private static UnintrusiveBindingPathProvider CreateTestSubject(ISolutionInfoProvider solutionInfoProvider,
            IEnvironmentVariableProvider envVars = null)
        {
            envVars ??= CreateEnvVars("any");
            return new UnintrusiveBindingPathProvider(solutionInfoProvider, envVars);
        }

        private Mock<ISolutionInfoProvider> CreateSolutionInfoProvider(string solutionName = null)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionName()).Returns(solutionName);

            return solutionInfoProvider;
        }

        private static IEnvironmentVariableProvider CreateEnvVars(string rootInstallPath)
        {
            var envVars = new Mock<IEnvironmentVariableProvider>();
            envVars.Setup(x => x.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(rootInstallPath);
            return envVars.Object;
        }
    }
}
