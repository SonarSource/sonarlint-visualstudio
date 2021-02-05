/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class VsVersionProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsVersionProvider, IVsVersionProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(CreateServiceProvider(Mock.Of<IVsShell>()))
            });
        }

        [TestMethod]
        public void Get_WrapsVsShellInformation()
        {
            object name = "some name";
            object shortName = "some short name";
            object build = "some build";

            var vsShell = new Mock<IVsShell>();
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID5.VSSPROPID_AppBrandName, out name));
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID5.VSSPROPID_AppShortBrandName, out shortName));
            vsShell.Setup(x => x.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out build));

            var testSubject = new VsVersionProvider(CreateServiceProvider(vsShell.Object));

            testSubject.Version.Should().NotBeNull();
            testSubject.Version.Name.Should().Be((string) name);
            testSubject.Version.ShortName.Should().Be((string) shortName);
            testSubject.Version.BuildVersion.Should().Be((string) build);
        }

        private IServiceProvider CreateServiceProvider(IVsShell vsShell)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShell))).Returns(vsShell);

            return serviceProvider.Object;
        }
    }
}
