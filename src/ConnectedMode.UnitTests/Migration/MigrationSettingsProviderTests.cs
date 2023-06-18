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

using SonarLint.VisualStudio.ConnectedMode.Migration;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class MigrationSettingsProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MigrationSettingsProvider, IMigrationSettingsProvider>();
        }

        [TestMethod]
        public void MefCtor_CheckTypeIsNonShared()
            => MefTestHelpers.CheckIsNonSharedMefComponent<MigrationSettingsProvider>();

        [TestMethod]
        public async Task Get_ReturnsExpectedValue()
        {
            var testSubject = CreateTestSubject();

            // TODO - fix once implemented
            var actual = await testSubject.GetAsync("expected project key");
            actual.Should().BeNull();
        }

        private static MigrationSettingsProvider CreateTestSubject() => new MigrationSettingsProvider();
    }
}
