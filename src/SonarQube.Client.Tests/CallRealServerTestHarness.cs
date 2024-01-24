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

using System;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;
using SonarQube.Client.Tests.Infra;

// Dummy tests. These exist to make it easy to make real SonarQube/SonarCloud
// calls during development
//
// Usage:
// * uncomment the [TestClass] attribute
// * if calling SonarCloud, set a valid SonarCloud token

namespace SonarQube.Client.Tests
{
    //[TestClass]
    public class CallRealServerTestHarness
    {
        [TestMethod]
        public async Task Call_Real_SonarQube()
        {
            var url = new Uri("http://localhost:9000");

            string userName = "admin";
            var password = new SecureString();
            password.AppendChar('a');
            password.AppendChar('d');
            password.AppendChar('m');
            password.AppendChar('i');
            password.AppendChar('n');

            var connInfo = new ConnectionInformation(url, userName, password);

            var service = new SonarQubeService(new HttpClientHandler(), "agent", new TestLogger());
            try
            {
                await service.ConnectAsync(connInfo, CancellationToken.None);

                // Example
                string fileKey = "junk:MyClass.cs";
                var result = await service.GetSourceCodeAsync(fileKey, CancellationToken.None);
                result.Should().NotBeNullOrEmpty();
            }
            finally
            {
                service.Disconnect();
            }
        }

        [TestMethod]
        public async Task Call_Real_SonarCloud()
        {
            // TODO: set to a valid SonarCloud token but make sure you don't check it in...
            string validSonarCloudToken = "DO NOT CHECK IN A REAL TOKEN";

            var url = ConnectionInformation.FixedSonarCloudUri;
            var password = new SecureString();
            var connInfo = new ConnectionInformation(url, validSonarCloudToken, password);

            var service = new SonarQubeService(new HttpClientHandler(), "agent", new TestLogger());
            try
            {
                await service.ConnectAsync(connInfo, CancellationToken.None);

                // Example
                var fileKey = "vuln:Tools/Orchard/Logger.cs";
                var result = await service.GetSourceCodeAsync(fileKey, CancellationToken.None);
                result.Should().NotBeNullOrEmpty();
            }
            finally
            {
                service.Disconnect();
            }
        }
    }
}
