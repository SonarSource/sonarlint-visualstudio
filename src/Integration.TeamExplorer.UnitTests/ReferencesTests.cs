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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ReferencesTests
    {
        [TestMethod]
        public void MicrosoftTeamFoundationClient_EnsureCorrectVersion()
        {
            var expectedDllVersions = new Dictionary<string, int>
            {
                { "VS2019", 16 },
                { "VS2022", 16 } // 2022's dll is still 16 and not 17
            };

            var tfClientAssemblyVersion = AssemblyHelper.GetVersionOfReferencedAssembly(
                    typeof(TeamExplorerController), "Microsoft.TeamFoundation.Client");

            AssertIsCorrectMajorVersion(tfClientAssemblyVersion.Major, expectedDllVersions);
        }

        [TestMethod]
        public void MicrosoftTeamFoundationControls_EnsureCorrectVersion()
        {
            var expectedDllVersions = new Dictionary<string, int>
            {
                { "VS2019", 16 },
                { "VS2022", 17 }
            };

            var tfControlsAssemblyVersion = AssemblyHelper.GetVersionOfReferencedAssembly(
                    typeof(TeamExplorerController), "Microsoft.TeamFoundation.Controls");

            AssertIsCorrectMajorVersion(tfControlsAssemblyVersion.Major, expectedDllVersions);
        }

        private void AssertIsCorrectMajorVersion(int dllMajorVersion, Dictionary<string, int> expectedDllVersions)
        {
            var executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            executingAssemblyLocation.Should()
                .NotBeNull()
                .And.EndWith("Integration.TeamExplorer.UnitTests.dll");

            var expectedVersion = expectedDllVersions.Single(x => executingAssemblyLocation.Contains(x.Key)).Value;

            dllMajorVersion.Should().Be(expectedVersion);
        }
    }
}
