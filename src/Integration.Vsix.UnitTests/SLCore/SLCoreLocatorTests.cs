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

using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.UnitTests.SLCore
{
    [TestClass]
    public class SLCoreLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreLocator, ISLCoreLocator>(MefTestHelpers.CreateExport<IVsixRootLocator>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreLocator>();
        }

        [TestMethod]
        public void LocateExecutable_ReturnsLaunchParameters()
        {
            var vsixRootLocator = Substitute.For<IVsixRootLocator>();
            vsixRootLocator.GetVsixRoot().Returns("C:\\SomePath");

            var testSubject = new SLCoreLocator(vsixRootLocator);

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be("""C:\SomePath\Sloop\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be("""-classpath "C:\SomePath\Sloop\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }
        
        [TestMethod]
        public void LocateExecutable_CustomVsixFoler_IsIncludedInPath()
        {
            var vsixRootLocator = Substitute.For<IVsixRootLocator>();
            vsixRootLocator.GetVsixRoot().Returns("C:\\SomePath");

            var testSubject = new SLCoreLocator(vsixRootLocator, "Custom\\VsixSubpath\\");

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be("""C:\SomePath\Custom\VsixSubpath\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be("""-classpath "C:\SomePath\Custom\VsixSubpath\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }
        
        [TestMethod]
        public void LocateExecutable_EmptyVsixSubPath_UsesVsixRootDirectly()
        {
            var vsixRootLocator = Substitute.For<IVsixRootLocator>();
            vsixRootLocator.GetVsixRoot().Returns("C:\\SomePath");

            var testSubject = new SLCoreLocator(vsixRootLocator, string.Empty);

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be("""C:\SomePath\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be("""-classpath "C:\SomePath\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }
    }
}
