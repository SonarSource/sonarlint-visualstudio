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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class BoundSonarQubeProjectTests
    {
        [TestMethod]
        public void BoundSonarQubeProject_Serialization()
        {
            // Arrange
            var serverUri = new Uri("https://finding-nemo.org");
            var projectKey = "MyProject Key";
            var testSubject = new BoundSonarQubeProject(serverUri, projectKey, new BasicAuthCredentials("used", "pwd".ToSecureString()));

            // Act (serialize + de-serialize)
            string data = JsonHelper.Serialize(testSubject);
            BoundSonarQubeProject deserialized = JsonHelper.Deserialize<BoundSonarQubeProject>(data);

            // Assert
            deserialized.Should().NotBe(testSubject);
            deserialized.ProjectKey.Should().Be(testSubject.ProjectKey);
            deserialized.ServerUri.Should().Be(testSubject.ServerUri);
            deserialized.Credentials.Should().BeNull();
        }
    }
}