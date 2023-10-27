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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared
{
    [TestClass]
    public class SharedBindingConfigModelTests
    {
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
        public void GetServerType_NoOrganization_ReturnsQube()
        {
            SharedBindingConfigModel model = new SharedBindingConfigModel{ProjectKey = "abc", Uri = "https://next.sonarqube.com"};

            model.GetServerType().Should().Be(ServerType.SonarQube);
        }
        
        [TestMethod]
        public void GetServerType_HasOrganization_ReturnsCloud()
        {
            SharedBindingConfigModel model = new SharedBindingConfigModel{ProjectKey = "abc", Organization = "my org"};

            model.GetServerType().Should().Be(ServerType.SonarCloud);
        }
    }
}
