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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class UnintrusiveBindingPathProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
            => MefTestHelpers.CheckTypeCanBeImported<UnintrusiveBindingPathProvider, IUnintrusiveBindingPathProvider>();

        [TestMethod]
        public void MefCtor_CheckIsNonSharedMefComponent()
            => MefTestHelpers.CheckIsNonSharedMefComponent<UnintrusiveBindingController>();

        [TestMethod]
        public void Get_NoOpenSolution_ReturnsNull()
        {
            var testSubject = CreateTestSubject();

            var actual = testSubject.GetBindingPath(null);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void Get_HasOpenSolution_ReturnsExpectedValue()
        {
            const string solutionName = "mysolutionName";
            const string rootFolderName = @"x:\users\foo\";

            var envVars = CreateEnvVars(rootFolderName);

            var testSubject = CreateTestSubject(envVars);

            var actual = testSubject.GetBindingPath(solutionName);

            actual.Should().Be($@"{rootFolderName}SonarLint for Visual Studio\Bindings\{solutionName}\binding.config");
        }

        [TestMethod]
        public void GetBindingKeyFromPath_ReturnsBindingFolderName()
        {
            const string solutionName = "mysolutionName";
            const string rootFolderName = @"x:\users\foo\";

            var envVars = CreateEnvVars(rootFolderName);

            var testSubject = CreateTestSubject(envVars);

            var actual = testSubject.GetBindingKeyFromPath($@"{rootFolderName}SonarLint for Visual Studio\Bindings\{solutionName}\binding.config");

            actual.Should().Be(solutionName);
        }

        [TestMethod]
        public void GetBindingFolders_NoBindingFolder_ReturnsEmpy()
        {
            const string rootFolderName = @"x:\users\foo\";

            var envVars = CreateEnvVars(rootFolderName);

            var testSubject = CreateTestSubject(envVars);

            var actual = testSubject.GetBindingPaths();

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetBindingFolders_NoBindings_ReturnsEmpty()
        {
            const string rootFolderName = @"x:\users\foo\";

            var envVars = CreateEnvVars(rootFolderName);

            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory($@"{rootFolderName}SonarLint for Visual Studio\Bindings\");

            var testSubject = CreateTestSubject(envVars: envVars, fileSystem: fileSystem);

            var actual = testSubject.GetBindingPaths();

            actual.Should().NotBeNull();
            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetBindingFolders_ReturnsBindings()
        {
            const string rootFolderName = @"x:\users\foo\";

            var envVars = CreateEnvVars(rootFolderName);

            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory($@"{rootFolderName}SonarLint for Visual Studio\Bindings\");
            fileSystem.AddDirectory($@"{rootFolderName}SonarLint for Visual Studio\Bindings\Binding1");
            fileSystem.AddDirectory($@"{rootFolderName}SonarLint for Visual Studio\Bindings\Binding2");

            var bindingFolders = new string[] { $@"{rootFolderName}SonarLint for Visual Studio\Bindings\Binding1\binding.config", $@"{rootFolderName}SonarLint for Visual Studio\Bindings\Binding2\binding.config" };

            var testSubject = CreateTestSubject(envVars: envVars, fileSystem: fileSystem);

            var actual = testSubject.GetBindingPaths();

            actual.Should().HaveCount(2);
            actual.Should().BeEquivalentTo(bindingFolders);
        }

        private static UnintrusiveBindingPathProvider CreateTestSubject(IEnvironmentVariableProvider envVars = null, IFileSystem fileSystem = null)
        {
            fileSystem ??= new MockFileSystem();
            envVars ??= CreateEnvVars("any");
            return new UnintrusiveBindingPathProvider(envVars, fileSystem);
        }

        private static IEnvironmentVariableProvider CreateEnvVars(string rootInstallPath)
        {
            var envVars = new Mock<IEnvironmentVariableProvider>();
            envVars.Setup(x => x.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(rootInstallPath);
            return envVars.Object;
        }
    }
}
