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
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class UnintrusiveBindingPathProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var serviceProvider = CreateConfiguredServiceProvider("");

            MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingPathProvider, IUnintrusiveBindingPathProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(serviceProvider.Object),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void Ctor_RunsOnUIThreadSync()
        {
            var serviceProvider = CreateConfiguredServiceProvider(null);
            var threadHandling = new Mock<IThreadHandling>();

            var testSubject = CreateTestSubject(serviceProvider.Object, threadHandling: threadHandling.Object);

#pragma warning disable CS0618 // Type or member is obsolete
            threadHandling.Verify(x => x.RunOnUIThreadSync(It.IsAny<Action>()), Times.Once);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [TestMethod]
        public void Get_NoOpenSolution_ReturnsNull()
        {
            var serviceProvider = CreateConfiguredServiceProvider(null);
            var testSubject = CreateTestSubject(serviceProvider.Object);

            var actual = testSubject.Get();
            actual.Should().BeNull();
        }

        [TestMethod]
        public void Get_HasOpenSolution_ReturnsExpectedValue()
        {
            const string solutionPath = @"c:\aaa\bbbb\C C";
            const string solutionName = "mysolutionName";
            const string rootFolderName = @"x:\users\foo\";

            var fullPathSlnPath = Path.Combine(solutionPath, solutionName + ".sln");

            var serviceProvider = CreateConfiguredServiceProvider(fullPathSlnPath);
            var envVars = CreateEnvVars(rootFolderName);

            var testSubject = CreateTestSubject(serviceProvider.Object, envVars);

            var actual = testSubject.Get();

            actual.Should().Be($@"{rootFolderName}SonarLint for Visual Studio\Bindings\{solutionName}_{solutionPath.GetHashCode()}\binding.config");
        }

        private static UnintrusiveBindingPathProvider CreateTestSubject(IServiceProvider serviceProvider,
            IEnvironmentVariableProvider envVars = null,
            IThreadHandling threadHandling = null)
        {
            envVars ??= CreateEnvVars("any");
            threadHandling ??= new NoOpThreadHandler();
            return new UnintrusiveBindingPathProvider(serviceProvider, threadHandling, envVars);
        }

        private Mock<IServiceProvider> CreateConfiguredServiceProvider(string fullSolutionFilePath = null)
        {
            var solution = new Mock<IVsSolution>();
            object objFilePath = fullSolutionFilePath;
            solution.Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out objFilePath));

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsSolution))).Returns(solution.Object);

            return serviceProvider;
        }

        private static IEnvironmentVariableProvider CreateEnvVars(string rootInstallPath)
        {
            var envVars = new Mock<IEnvironmentVariableProvider>();
            envVars.Setup(x => x.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(rootInstallPath);
            return envVars.Object;
        }
    }
}
