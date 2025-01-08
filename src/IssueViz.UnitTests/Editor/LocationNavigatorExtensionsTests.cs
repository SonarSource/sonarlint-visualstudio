﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using NSubstitute;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor;

[TestClass]
public class LocationNavigatorExtensionsTests
{
    [DataTestMethod]
    [DataRow(NavigationResult.Failed, false)]
    [DataRow(NavigationResult.OpenedFile, false)]
    [DataRow(NavigationResult.OpenedLocation, true)]
    public void TryNavigate_ReturnsValueFromNavigator(NavigationResult navigationResult, bool expectedResult)
    {
        var location = Substitute.For<IAnalysisIssueLocationVisualization>();
        var locationNavigator = Substitute.For<ILocationNavigator>();
        locationNavigator.TryNavigatePartial(location).Returns(navigationResult);

        LocationNavigatorExtensions.TryNavigate(locationNavigator, location).Should().Be(expectedResult);
    }
}
