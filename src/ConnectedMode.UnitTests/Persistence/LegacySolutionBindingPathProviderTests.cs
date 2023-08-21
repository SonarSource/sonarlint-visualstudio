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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence
{
    [TestClass]
    public class LegacySolutionBindingPathProviderTests
    {
        [TestMethod]
        public void Ctor_NullArgument_Exception()
        {
            Action act = () => new LegacySolutionBindingPathProvider(null);

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
        [DataRow(@"c:\test", @"c:\test\SonarQube\SolutionBinding.sqconfig")]
        [DataRow(@"c:\aaa\bbb", @"c:\aaa\bbb\SonarQube\SolutionBinding.sqconfig")]
        public void Get_ReturnsExpectedValue(string solutionDirectoryToReturn, string expectedResult)
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(solutionDirectoryToReturn);
            var testSubject = CreateTestSubject(solutionInfoProvider.Object);

            var actual = testSubject.Get();
            actual.Should().Be(expectedResult);
        }

        private static Mock<ISolutionInfoProvider> CreateSolutionInfoProvider(string solutionDirectoryToReturn)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionDirectory()).Returns(solutionDirectoryToReturn);
            return solutionInfoProvider;
        }

        private static LegacySolutionBindingPathProvider CreateTestSubject(ISolutionInfoProvider solutionInfoProvider)
        {
            var testSubject = new LegacySolutionBindingPathProvider(solutionInfoProvider ?? Mock.Of<ISolutionInfoProvider>());
            return testSubject;
        }
    }
}
