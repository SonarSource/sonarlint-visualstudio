/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
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
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Helpers
{
    [TestClass]
    public class SonarQubeIssueTypeConverterTests
    {
        [TestMethod]
        // Exact matches
        [DataRow("CODE_SMELL", SonarQubeIssueType.CodeSmell)]
        [DataRow("BUG", SonarQubeIssueType.Bug)]
        [DataRow("VULNERABILITY", SonarQubeIssueType.Vulnerability)]
        [DataRow("SECURITY_HOTSPOT", SonarQubeIssueType.SecurityHotspot)]

        // Case-insensitivity
        [DataRow("bug", SonarQubeIssueType.Bug)]
        [DataRow("security_hotspot", SonarQubeIssueType.SecurityHotspot)]

        // Unknowns
        [DataRow(null, SonarQubeIssueType.Unknown)]
        [DataRow("", SonarQubeIssueSeverity.Unknown)]
        [DataRow("BUGX", SonarQubeIssueSeverity.Unknown)]
        [DataRow("foo", SonarQubeIssueSeverity.Unknown)]
        public void Convert(string inputData, SonarQubeIssueType expectedResult)
        {
            SonarQubeIssueTypeConverter.Convert(inputData).Should().Be(expectedResult);
        }
    }
}
