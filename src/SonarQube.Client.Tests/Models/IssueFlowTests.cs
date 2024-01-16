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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class IssueFlowTests
    {
        [TestMethod]
        public void Ctor_LocationsAreNeverNull()
        {
            var testSubject = new IssueFlow(null);

            testSubject.Locations.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_PropertiesAreSet()
        {
            var locations = new List<IssueLocation>
            {
                new IssueLocation("file1", "component1", null, "message1"),
                new IssueLocation("file2", "component2", null, "message2")
            };

            var testSubject = new IssueFlow(locations);

            testSubject.Locations.Should().BeEquivalentTo(locations[0], locations[1]);
        }
    }
}
