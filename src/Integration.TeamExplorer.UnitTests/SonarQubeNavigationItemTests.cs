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

using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class SonarQubeNavigationItemTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
                => MefTestHelpers.CheckTypeCanBeImported<SonarQubeNavigationItem, ITeamExplorerNavigationItem>(
                    MefTestHelpers.CreateExport<ITeamExplorerController>());

        [TestMethod]
        public void CheckIsNonSharedMefComponent()
            => MefTestHelpers.CheckIsNonSharedMefComponent<SonarQubeNavigationItem>();

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var controller = new Mock<ITeamExplorerController>();

            _ = new SonarQubeNavigationItem(controller.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            controller.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Execute()
        {
            // Arrange
            var controller = new Mock<ITeamExplorerController>();

            var testSubject = CreateTestSubject(controller.Object);

            // Act
            testSubject.Execute();

            // Assert
            controller.Verify(x => x.ShowSonarQubePage(), Times.Once);
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor()
        {
            // Arrange & Act
            var testSubject = CreateTestSubject();

            // Assert
            testSubject.IsVisible.Should().BeTrue("Nav item should be visible");
            testSubject.IsEnabled.Should().BeTrue("Nav item should be enabled");
            testSubject.Text.Should().Be(Strings.TeamExplorerPageTitle, "Unexpected nav text");

            testSubject.Icon.Should().NotBeNull("Icon should not be null");
        }

        [TestMethod]
        public void SonarQubeNavigationItem_Ctor_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SonarQubeNavigationItem(null));
        }

        private static SonarQubeNavigationItem CreateTestSubject(ITeamExplorerController controller = null)
            => new SonarQubeNavigationItem(controller ?? Mock.Of<ITeamExplorerController>());
    }
}
