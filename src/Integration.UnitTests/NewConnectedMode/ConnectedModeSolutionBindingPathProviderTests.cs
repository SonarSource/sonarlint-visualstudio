/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConnectedModeSolutionBindingPathProviderTests
    {
        private Mock<IVsSolution> solution;
        private ConnectedModeSolutionBindingPathProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solution = new Mock<IVsSolution>();
            testSubject = new ConnectedModeSolutionBindingPathProvider(solution.Object);

        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolution_Throws()
        {
            // Arrange
            Action act = () => new ConnectedModeSolutionBindingPathProvider(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solution");
        }

        [TestMethod]
        public void Get_NoOpenSolution_ReturnsNull()
        {
            object fullSolutionFilePath = null;
            solution.Setup(x => x.GetProperty((int) __VSPROPID.VSPROPID_SolutionFileName, out fullSolutionFilePath));

            var actual = testSubject.Get();
            actual.Should().BeNull();
        }

        [TestMethod]
        public void Get_HasOpenSolution_ReturnsNull()
        {
            object fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.sln";
            solution.Setup(x => x.GetProperty((int) __VSPROPID.VSPROPID_SolutionFileName, out fullSolutionFilePath));

            var actual = testSubject.Get();
            
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.slconfig");
        }

        [TestMethod]
        public void Get_NullPath_ReturnsNull()
        {
            // Arrange & Act & Assert
            ConnectedModeSolutionBindingPathProvider.GetConnectionFilePath(null).Should().BeNull();
        }

        [TestMethod]
        public void Get_SolutionFilePath_ValidFilePath()
        {
            // Arrange
            var fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.sln";

            // Act
            string actual = ConnectedModeSolutionBindingPathProvider.GetConnectionFilePath(fullSolutionFilePath);

            // Assert
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.slconfig");
        }

        [TestMethod]
        public void Get_FilePath_ValidFilePath()
        {
            // Arrange
            var fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.foo.xxx";

            // Act
            string actual = ConnectedModeSolutionBindingPathProvider.GetConnectionFilePath(fullSolutionFilePath);

            // Assert
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.foo.slconfig");
        }
    }
}
