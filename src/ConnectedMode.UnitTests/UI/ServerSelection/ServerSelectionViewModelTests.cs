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

using System.ComponentModel;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI.ServerSelection
{
    [TestClass]
    public class ServerSelectionViewModelTests
    {
        private ServerSelectionViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new ServerSelectionViewModel(Substitute.For<IDogfoodingService>());
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
        public void IsNextButtonEnabled_SonarCloudIsSelectedAndNoRegionIsSelected_ReturnsFalse()
        {
            testSubject.IsSonarCloudSelected = true;
            testSubject.IsSonarQubeSelected = false;

            testSubject.IsEuRegionSelected = false;
            testSubject.IsUsRegionSelected = false;

            testSubject.IsNextButtonEnabled.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void IsNextButtonEnabled_SonarCloudIsSelectedAndRegionIsSelected_ReturnsTrue(bool isEuSelected, bool isUsSelected)
        {
            testSubject.IsSonarCloudSelected = true;
            testSubject.IsSonarQubeSelected = false;

            testSubject.IsEuRegionSelected = isEuSelected;
            testSubject.IsUsRegionSelected = isUsSelected;

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
        public void IsNextButtonEnabled_SonarQubeIsSelectedAndUrlIsProvided_ReturnsTrueAndIgnoresRegion()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;
            testSubject.IsEuRegionSelected = testSubject.IsUsRegionSelected = false;

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

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow("  asab")]
        public void ShowSecurityWarning_UrlInvalid_ReturnsFalse(string url)
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = url;

            testSubject.ShowSecurityWarning.Should().BeFalse();
        }

        [TestMethod]
        public void ShowSecurityWarning_UrlSecureProtocol_ReturnsFalse()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = "https://localhost:9000";

            testSubject.ShowSecurityWarning.Should().BeFalse();
        }

        [TestMethod]
        public void ShowSecurityWarning_UrlInsecureProtocol_ReturnsTrue()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;

            testSubject.SonarQubeUrl = "http://localhost:9000";

            testSubject.ShowSecurityWarning.Should().BeTrue();
        }

        [TestMethod]
        public void CreateTransientConnectionInfo_SonarQubeIsSelected_ReturnsConnectionWithSmartNotificationsEnabled()
        {
            testSubject.IsSonarCloudSelected = false;
            testSubject.IsSonarQubeSelected = true;
            testSubject.SonarQubeUrl = "http://localhost:90";

            var createdConnection = testSubject.CreateTransientConnectionInfo();

            createdConnection.Id.Should().Be(testSubject.SonarQubeUrl);
            createdConnection.ServerType.Should().Be(ConnectionServerType.SonarQube);
        }

        [TestMethod]
        public void CreateTransientConnectionInfo_SonarCloudIsSelected_ReturnsConnectionWithSmartNotificationsEnabled()
        {
            testSubject.IsSonarCloudSelected = true;
            testSubject.IsSonarQubeSelected = false;

            var createdConnection = testSubject.CreateTransientConnectionInfo();

            createdConnection.Id.Should().BeNull();
            createdConnection.ServerType.Should().Be(ConnectionServerType.SonarCloud);
        }

        [TestMethod]
        public void IsEuRegionSelected_ShouldBeTrueByDefault()
        {
            testSubject.IsEuRegionSelected.Should().BeTrue();
            testSubject.IsUsRegionSelected.Should().BeFalse();
        }

        [TestMethod]
        public void IsEuRegionSelected_Set_RaisesEvents()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            testSubject.IsEuRegionSelected = !testSubject.IsEuRegionSelected;

            eventHandler.Received().Invoke(testSubject,
                Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsEuRegionSelected)));
            eventHandler.Received().Invoke(testSubject,
                Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsNextButtonEnabled)));
        }

        [TestMethod]
        public void IsUsRegionSelected_Set_RaisesEvents()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            testSubject.IsUsRegionSelected = !testSubject.IsUsRegionSelected;

            eventHandler.Received().Invoke(testSubject,
                Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsUsRegionSelected)));
            eventHandler.Received().Invoke(testSubject,
                Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.IsNextButtonEnabled)));
        }
    }
}
