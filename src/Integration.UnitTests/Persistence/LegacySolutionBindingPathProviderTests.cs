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
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacySolutionBindingPathProviderTests
    {
        private Mock<IVsSolution> solution;
        private LegacySolutionBindingPathProvider testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solution = new Mock<IVsSolution>();
            testSubject = new LegacySolutionBindingPathProvider(solution.Object);
        }

        [TestMethod]
        public void Ctor_NullArgument_Exception()
        {
            Action act = () => new LegacySolutionBindingPathProvider(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("vsSolution");
        }

        [TestMethod]
        public void Get_SolutionFolderIsNull_Null()
        {
            SetupSolutionInfoResult(null);

            var actual = testSubject.Get();

            actual.Should().Be(null);
        }

        [TestMethod]
        public void Get_SolutionFolderIsNotNull_FilePathUnderSolutionFolder()
        {
            SetupSolutionInfoResult("c:\\test");

            var actual = testSubject.Get();

            var expected = Path.Combine("c:\\test", Constants.LegacySonarQubeManagedFolderName, LegacySolutionBindingPathProvider.LegacyBindingConfigurationFileName);

            actual.Should().Be(expected);
        }

        private void SetupSolutionInfoResult(string solutionDirectory)
        {
            var solutionName = "test";
            var userOptionsFile = "dummy";

            solution
                .Setup(x => x.GetSolutionInfo(out solutionDirectory, out solutionName, out userOptionsFile))
                .Returns(0);
        }
    }
}
