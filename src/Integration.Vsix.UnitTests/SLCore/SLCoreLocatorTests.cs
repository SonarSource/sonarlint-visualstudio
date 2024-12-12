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

using System.IO.Abstractions;
using Org.BouncyCastle.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.SLCore;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.Integration.Vsix.UnitTests.SLCore
{
    [TestClass]
    public class SLCoreLocatorTests
    {
        private IVsixRootLocator vsixRootLocator;
        private ISonarLintSettings settings;
        private SLCoreLocator testSubject;
        private ILogger logger;
        private IFileSystem fileSystem;
        private const string VsixRoot = "C:\\SomePath";

        [TestInitialize]
        public void TestInitialize()
        {
            vsixRootLocator = Substitute.For<IVsixRootLocator>();
            settings = Substitute.For<ISonarLintSettings>();
            logger = Substitute.For<ILogger>();
            fileSystem = Substitute.For<IFileSystem>();
            testSubject = new SLCoreLocator(vsixRootLocator, string.Empty, settings, logger, fileSystem);

            vsixRootLocator.GetVsixRoot().Returns(VsixRoot);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreLocator, ISLCoreLocator>(
                MefTestHelpers.CreateExport<IVsixRootLocator>(),
                MefTestHelpers.CreateExport<ISonarLintSettings>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreLocator>();
        }

        [TestMethod]
        public void LocateExecutable_ReturnsLaunchParameters()
        {
            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be($"""{VsixRoot}\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }
        
        [TestMethod]
        public void LocateExecutable_CustomVsixFoler_IsIncludedInPath()
        {
            var slCoreLocator = new SLCoreLocator(vsixRootLocator, "Custom\\VsixSubpath\\", settings, logger, fileSystem);

            var result = slCoreLocator.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be($"""{VsixRoot}\Custom\VsixSubpath\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\Custom\VsixSubpath\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }
        
        [TestMethod]
        public void LocateExecutable_EmptyVsixSubPath_UsesVsixRootDirectly()
        {
            vsixRootLocator.GetVsixRoot().Returns("C:\\SomePath");
            var slCoreLocator = new SLCoreLocator(vsixRootLocator, string.Empty, settings, logger, fileSystem);

            var result = slCoreLocator.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be($"""{VsixRoot}\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void LocateExecutable_JreLocationIsNotProvided_UsesBundledJre(string emptyJreLocation)
        {
            settings.JreLocation.Returns(emptyJreLocation);

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be($"""{VsixRoot}\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
        }

        [TestMethod]
        public void LocateExecutable_JreLocationIsProvidedAndExeCanNotBeDetected_UsesBundledJreAndLogs()
        {
            settings.JreLocation.Returns("C:\\jrePath");
            var customPathToExecutable = """C:\jrePath\bin\java.exe""";
            fileSystem.File.Exists(customPathToExecutable).Returns(false);

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be($"""{VsixRoot}\jre\bin\java.exe""");
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
            logger.Received(1).LogVerbose(string.Format(Resources.Strings.SlCoreLocator_CustomJreLocationNotFound, customPathToExecutable));
        }

        [TestMethod]
        public void LocateExecutable_JreLocationIsProvidedAndExeCanBeDetected_UsesProvidedJreLocationAndLogs()
        {
            settings.JreLocation.Returns("C:\\jrePath");
            var expectedPathToExecutable = """C:\jrePath\bin\java.exe""";
            fileSystem.File.Exists(expectedPathToExecutable).Returns(true);

            var result = testSubject.LocateExecutable();

            result.Should().NotBeNull();
            result.PathToExecutable.Should().Be(expectedPathToExecutable);
            result.LaunchArguments.Should().Be($"""-classpath "{VsixRoot}\lib\*" org.sonarsource.sonarlint.core.backend.cli.SonarLintServerCli""");
            logger.Received(1).LogVerbose(string.Format(Resources.Strings.SlCoreLocator_UsingCustomJreLocation, expectedPathToExecutable));
        }
    }
}
