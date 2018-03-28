/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Api.Requests.V6_30;

namespace SonarQube.Client.Tests.Api.Requests.V6_30
{
    [TestClass]
    public class GetPluginsRequest_FormatForSonarLint
    {
        [TestMethod]
        public void FormatForSonarLint_Returns_Expected_Values()
        {
            GetPluginsRequest.FormatForSonarLint("invalid").Should().Be("invalid"); // invalid version, returns the same

            GetPluginsRequest.FormatForSonarLint("1").Should().Be("1.0.0");
            GetPluginsRequest.FormatForSonarLint("1.2").Should().Be("1.2.0");
            GetPluginsRequest.FormatForSonarLint("1.2.3").Should().Be("1.2.3");
            GetPluginsRequest.FormatForSonarLint("1.2.3.4").Should().Be("1.2.3");

            GetPluginsRequest.FormatForSonarLint("1 (Build 2345)").Should().Be("1.0.0");
            GetPluginsRequest.FormatForSonarLint("1 (build 2345)").Should().Be("1.0.0"); // lower case B
            GetPluginsRequest.FormatForSonarLint("1.2 (Build 3456)").Should().Be("1.2.0");
            GetPluginsRequest.FormatForSonarLint("1.2.3 (Build 4567)").Should().Be("1.2.3");
        }
    }
}
