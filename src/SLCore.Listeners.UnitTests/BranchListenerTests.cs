/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Branch;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests
{
    [TestClass]
    public class BranchListenerTests
    {
        private IStatefulServerBranchProvider statefulServerBranchProvider;
        private BranchListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            statefulServerBranchProvider = Substitute.For<IStatefulServerBranchProvider>();
            testSubject = new BranchListener(statefulServerBranchProvider);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BranchListener, ISLCoreListener>(
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>());
        }

        [TestMethod]
        public void Mef_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<BranchListener>();
        }

        [TestMethod]
        public async Task MatchSonarProjectBranch_ReturnsCalculatedBranch()
        {
            const string checkedOutBranch = "branch2";

            statefulServerBranchProvider.GetServerBranchNameAsync(CancellationToken.None).Returns(checkedOutBranch);

            var param = new MatchSonarProjectBranchParams("scopeId",
                "mainBranch",
                ["branch1", "branch2", "mainBranch"]);

            var result = await testSubject.MatchSonarProjectBranchAsync(param);

            result.matchedSonarBranch.Should().Be(checkedOutBranch);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(5)]
        [DataRow("something")]
        public void DidChangeMatchedSonarProjectBranch_ReturnsTaskCompleted(object parameter)
        {
            var result = testSubject.DidChangeMatchedSonarProjectBranchAsync(parameter);

            result.Should().Be(Task.CompletedTask);
        }
    }
}
