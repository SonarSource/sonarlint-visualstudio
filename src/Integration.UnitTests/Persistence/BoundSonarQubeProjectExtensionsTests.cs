/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BoundSonarQubeProjectExtensionsTests
    {
        [TestMethod]
        public void CreateConnectionInformation_ArgCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => BoundSonarQubeProjectExtensions.CreateConnectionInformation(null));
        }

        [TestMethod]
        public void CreateConnectionInformation_NoCredentials()
        {
            // Arrange
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey");

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Assert
            conn.ServerUri.Should().Be(input.ServerUri);
            conn.UserName.Should().BeNull();
            conn.Password.Should().BeNull();
        }

        [TestMethod]
        public void CreateConnectionInformation_BasicAuthCredentials()
        {
            // Arrange
            var creds = new BasicAuthCredentials("UserName", "password".ToSecureString());
            var input = new BoundSonarQubeProject(new Uri("http://server"), "ProjectKey", creds);

            // Act
            ConnectionInformation conn = input.CreateConnectionInformation();

            // Assert
            conn.ServerUri.Should().Be(input.ServerUri);
            conn.UserName.Should().Be(creds.UserName);
            conn.Password.ToUnsecureString().Should().Be(creds.Password.ToUnsecureString());
        }
    }
}