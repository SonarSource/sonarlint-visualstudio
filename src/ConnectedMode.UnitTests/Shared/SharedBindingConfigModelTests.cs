/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared
{
    [TestClass]
    public class SharedBindingConfigModelTests
    {
        private readonly SharedBindingConfigModel sonarQubeModel = new() { Uri = new Uri("https://localhost:9000"), ProjectKey = "abc" };
        private readonly SharedBindingConfigModel sonarCloudModel = new() { ProjectKey = "abc", Organization = "my org" };

        [DataRow("some Organisation", true)]
        [DataRow("    ", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [TestMethod]
        public void IsSonarCloud(string organization, bool expectedValue)
        {
            var testSubject = new SharedBindingConfigModel() { Organization = organization };

            var result = testSubject.IsSonarCloud();

            result.Should().Be(expectedValue);
        }

        [TestMethod]
        public void GetServerType_Null_ReturnsNull()
        {
            SharedBindingConfigModel model = null;

            model.GetServerType().Should().BeNull();
        }

        [TestMethod]
        public void GetServerType_NoOrganization_ReturnsQube() => sonarQubeModel.GetServerType().Should().Be(ConnectionServerType.SonarQube);

        [TestMethod]
        public void GetServerType_HasOrganization_ReturnsCloud() => sonarCloudModel.GetServerType().Should().Be(ConnectionServerType.SonarCloud);

        [TestMethod]
        public void CreateConnectionInfo_NoOrganization_ReturnsQube()
        {
            var result = sonarQubeModel.CreateConnectionInfo();

            result.ServerType.Should().Be(ConnectionServerType.SonarQube);
            result.Id.Should().Be(sonarQubeModel.Uri.ToString());
        }

        [TestMethod]
        public void CreateConnectionInfo_HasOrganizationAndNoRegion_ReturnsCloudForEu()
        {
            var result = sonarCloudModel.CreateConnectionInfo();

            result.ServerType.Should().Be(ConnectionServerType.SonarCloud);
            result.Id.Should().Be(sonarCloudModel.Organization);
            result.CloudServerRegion.Should().Be(CloudServerRegion.Eu);
        }

        [TestMethod]
        [DynamicData(nameof(GetCloudServerRegions), DynamicDataSourceType.Method)]
        public void CreateConnectionInfo_HasOrganizationAndRegion_ReturnsCloud(CloudServerRegion region)
        {
            sonarCloudModel.Region = region.Name;

            var result = sonarCloudModel.CreateConnectionInfo();

            result.ServerType.Should().Be(ConnectionServerType.SonarCloud);
            result.Id.Should().Be(sonarCloudModel.Organization);
            result.CloudServerRegion.Should().Be(region);
        }

        public static IEnumerable<object[]> GetCloudServerRegions() => [[CloudServerRegion.Eu], [CloudServerRegion.Us],];
    }
}
