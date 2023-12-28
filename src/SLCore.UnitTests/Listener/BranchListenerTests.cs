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

using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener
{
    [TestClass]
    public class BranchListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BranchListener, ISLCoreListener>();
        }

        [TestMethod]
        public void Mef_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<BranchListener>();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(5)]
        [DataRow("something")]
        public async Task MatchSonarProjectBranch_ReturnsNull(object parameter)
        {
            var testSubject = new BranchListener();

            var result = await testSubject.MatchSonarProjectBranch(parameter);

            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(5)]
        [DataRow("something")]
        public void DidChangeMatchedSonarProjectBranch_ReturnsTaskCompleted(object parameter)
        {
            var testSubject = new BranchListener();

            var result = testSubject.DidChangeMatchedSonarProjectBranch(parameter);

            result.Should().Be(Task.CompletedTask);
        }
    }
}
