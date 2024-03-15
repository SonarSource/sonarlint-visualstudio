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
using NSubstitute;
using SonarLint.VisualStudio.SLCore.Configuration;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Configuration
{
    [TestClass]
    public class VsixRootLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsixRootLocator, IVsixRootLocator>();
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<VsixRootLocator>();
        }

        [TestMethod]
        public void GetVsixRoot_ReturnsCorrectPath()
        {
            var path = Substitute.For<IPath>();
            path.GetDirectoryName(Arg.Any<string>()).Returns("C:\\SomePath");

            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.Path.Returns(path);

            var testSubject = new VsixRootLocator(fileSystem);

            testSubject.GetVsixRoot().Should().Be("C:\\SomePath");
        }
    }
}
