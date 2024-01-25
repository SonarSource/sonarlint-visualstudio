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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Helpers;

[TestClass]
public class SonarQubeIssueTransitionExtensionsTests
{
    [DataTestMethod]
    [DataRow(SonarQubeIssueTransition.FalsePositive, "falsepositive")]
    [DataRow(SonarQubeIssueTransition.WontFix, "wontfix")]
    [DataRow(SonarQubeIssueTransition.Accept, "accept")]
    public void TransitionToLowerCaseString_ConvertsEnumToLowerCaseString(SonarQubeIssueTransition transition, string expectedString)
    {
        transition.TransitionToLowerCaseString().Should().Be(expectedString);
    }
}
