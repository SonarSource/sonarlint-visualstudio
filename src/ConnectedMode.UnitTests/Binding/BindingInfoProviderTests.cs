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

using System.IO.Abstractions.TestingHelpers;
using SonarLint.VisualStudio.ConnectedMode.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingInfoProviderTests
    {
        [TestMethod]
        public void GetExistingBindings_BindingFolderDoNotExist_ReturnsEmpty()
        {
            var fileSytem = new MockFileSystem();

            var testSubject = CreateTestSubject(fileSytem);

            var result = testSubject.GetExistingBindings();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetExistingBindings_NoBindings_ReturnsEmpty()
        {
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");

            var testSubject = CreateTestSubject(fileSytem);

            var result = testSubject.GetExistingBindings();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetExistingBindings_HaveBindings_ReturnsBinding()
        {
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");
            fileSytem.AddDirectory("C:\\Bindings\\Binding1");
            fileSytem.AddDirectory("C:\\Bindings\\Binding2");

            var file1 = CreateFileData("https://sonarqube.somedomain.com", null, "projectKey1");
            var file2 = CreateFileData("https://sonarcloud.io", "organisation2", "projectKey2");

            fileSytem.AddFile("C:\\Bindings\\Binding1\\binding.config", file1);
            fileSytem.AddFile("C:\\Bindings\\Binding2\\binding.config", file2);

            var testSubject = CreateTestSubject(fileSytem);

            var result = testSubject.GetExistingBindings();

            result.Should().HaveCount(2);
            result[0].ServerUri.Should().Be("https://sonarqube.somedomain.com");
            result[0].Organization.Should().BeNull();
            result[0].ProjectKey.Should().Be("projectKey1");
            result[1].ServerUri.Should().Be("https://sonarcloud.io");
            result[1].Organization.Should().BeNull("organisation2");
            result[1].ProjectKey.Should().Be("projectKey2");
        }

        [TestMethod]
        public void GetExistingBindings_BindingConfigMissing_SkipFile()
        {
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");
            fileSytem.AddDirectory("C:\\Bindings\\Binding1");
            fileSytem.AddDirectory("C:\\Bindings\\Binding2");

            var file = CreateFileData("https://sonarcloud.io", "organisation", "projectKey");

            fileSytem.AddFile("C:\\Bindings\\Binding2\\binding.config", file);

            var testSubject = CreateTestSubject(fileSytem);

            var result = testSubject.GetExistingBindings();

            result.Should().HaveCount(1);
        }

        private static IUnintrusiveBindingPathProvider CreateUnintrusiveBindingPathProvider()
        {
            var unintrusiveBindingPathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            unintrusiveBindingPathProvider.SetupGet(u => u.SLVSRootBindingFolder).Returns("C:\\Bindings");
            return unintrusiveBindingPathProvider.Object;
        }

        private static BindingInfoProvider CreateTestSubject(MockFileSystem fileSytem)
        {
            var unintrusiveBindingPathProvider = CreateUnintrusiveBindingPathProvider();

            var testSubject = new BindingInfoProvider(unintrusiveBindingPathProvider, fileSytem);
            return testSubject;
        }

        private static MockFileData CreateFileData(string serverUri, string organization, string projectKey)
        {
            var content = organization != null
                ? $"{{\"ServerUri\":\"{serverUri}\",\"Organization\":{{\"Key\":\"{organization}\",\"Name\":\"\"}},\"ProjectKey\":\"{projectKey}\",\"ProjectName\":\"\"}}"
                : $"{{\"ServerUri\":\"{serverUri}\",\"Organization\":{{\"Key\":null,\"Name\":\"\"}},\"ProjectKey\":\"{projectKey}\",\"ProjectName\":\"\"}}";

            return new MockFileData(content);
        }
    }
}
