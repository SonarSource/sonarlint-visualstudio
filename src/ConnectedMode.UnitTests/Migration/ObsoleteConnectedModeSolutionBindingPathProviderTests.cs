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
using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class ObsoleteConnectedModeSolutionBindingPathProviderTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            // Arrange
            Action act = () => new ObsoleteConnectedModeSolutionBindingPathProvider(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionInfoProvider");
        }

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
        [DataRow(null, null)] // null solution -> solution not open -> return null
        [DataRow(@"c:\aaa\bbbb\C C\mysolutionName.sln", @"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.slconfig")]
        [DataRow(@"c:\aaa\bbbb\C C\mysolutionName.foo.xxx", @"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.foo.slconfig")]
        public void Get_ReturnsExpectedValue(string solutionPathToReturn, string expectedResult)
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(solutionPathToReturn);
            var testSubject = CreateTestSubject(solutionInfoProvider.Object);

            var actual = testSubject.Get();
            actual.Should().Be(expectedResult);
        }

        private static Mock<ISolutionInfoProvider> CreateSolutionInfoProvider(string solutionFilePathToReturn)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetFullSolutionFilePath()).Returns(solutionFilePathToReturn);
            return solutionInfoProvider;
        }

        private static ObsoleteConnectedModeSolutionBindingPathProvider CreateTestSubject(ISolutionInfoProvider solutionInfoProvider)
        {
            var testSubject = new ObsoleteConnectedModeSolutionBindingPathProvider(solutionInfoProvider ?? Mock.Of<ISolutionInfoProvider>());
            return testSubject;
        }
    }
}
