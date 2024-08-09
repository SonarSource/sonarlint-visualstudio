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

using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ServerSelection
{
    [TestClass]
    public class ServerSelectionViewModelTests
    {
        private ServerSelectionViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new ServerSelectionViewModel();
        }

        [TestMethod]
        public void IsSonarCloudSelected_ShouldBeTrueByDefault()
        {
            testSubject.IsSonarCloudSelected.Should().BeTrue();
            testSubject.IsSonarQubeSelected.Should().BeFalse();
        }

        [TestMethod]
        public void IsNextButtonEnabled_NoServerIsSelected_ReturnsFalse()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = false;

            testSubject.IsNextButtonEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void IsNextButtonEnabled_SonarCloudIsSelected_ReturnsTrue()
        {
            testSubject.IsSonarCloudSelected = true;
            testSubject.IsSonarQubeSelected = false;

            testSubject.IsNextButtonEnabled.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void IsNextButtonEnabled_SonarQubeIsSelectedAndNoUrlProvided_ReturnsFalse(string url)
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = url;

            testSubject.IsNextButtonEnabled.Should().BeFalse();
        }

        [TestMethod]
        public void IsNextButtonEnabled_SonarQubeIsSelectedAndUrlIsProvided_ReturnsTrue()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = "dummy URL";

            testSubject.IsNextButtonEnabled.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSonarQubeUrlBeFilled_SonarCloudIsSelected_ReturnsFalse()
        {
            testSubject.IsSonarCloudSelected = true;
            testSubject.IsSonarQubeSelected = false;

            testSubject.ShouldSonarQubeUrlBeFilled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void ShouldSonarQubeUrlBeFilled_SonarQubeIsSelectedAndUrlIsEmpty_ReturnsTrue(string url)
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = url;

            testSubject.ShouldSonarQubeUrlBeFilled.Should().BeTrue();
        }

        [TestMethod]
        public void ShouldSonarQubeUrlBeFilled_SonarQubeIsSelectedAndUrlIsNotEmpty_ReturnsFalse()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = "dummy url";

            testSubject.ShouldSonarQubeUrlBeFilled.Should().BeFalse();
        }
    }
}
