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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Connection;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Connection;

[TestClass]
public class ListUserOrganizationsParamsTests
{

    [DataRow(SonarCloudRegion.EU, """
                                  {
                                    "credentials": {
                                      "username": "myUser",
                                      "password": "password"
                                    },
                                    "region": "EU"
                                  }
                                  """)]
    [DataRow(SonarCloudRegion.US, """
                                  {
                                    "credentials": {
                                      "username": "myUser",
                                      "password": "password"
                                    },
                                    "region": "US"
                                  }
                                  """)]
    [TestMethod]
    public void Ctor_UsernamePasswordIsUsed_SerializeAsExpected(SonarCloudRegion region, string expectedString)
    {
        var testSubject = new ListUserOrganizationsParams(new UsernamePasswordDto("myUser", "password"), region);

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [DataRow(SonarCloudRegion.EU, """
                                  {
                                    "credentials": {
                                      "token": "mytoken"
                                    },
                                    "region": "EU"
                                  }
                                  """)]
    [DataRow(SonarCloudRegion.US, """
                                  {
                                    "credentials": {
                                      "token": "mytoken"
                                    },
                                    "region": "US"
                                  }
                                  """)]
    [TestMethod]
    public void Ctor_TokenIsUsed_SerializeAsExpected(SonarCloudRegion region, string expectedString)
    {
        var testSubject = new ListUserOrganizationsParams(new TokenDto("mytoken"), region);

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }
}
