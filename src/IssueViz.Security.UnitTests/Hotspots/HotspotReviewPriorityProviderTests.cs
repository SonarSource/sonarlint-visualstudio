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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class HotspotReviewPriorityProviderTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<HotspotReviewPriorityProvider, IHotspotReviewPriorityProvider>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<HotspotReviewPriorityProvider>();
    }

    [DataRow("notakey", null)]
    [DataRow("typescript:S4502", HotspotPriority.High)]
    [DataRow("javascript:S4502", HotspotPriority.High)]
    [DataRow("typescript:S1313", HotspotPriority.Low)]
    [DataRow("javascript:S1313", HotspotPriority.Low)]
    [DataRow("javascript:S90000000000000000", null)]
    [DataRow("c:S1313", HotspotPriority.Low)]
    [DataRow("cpp:S1313", HotspotPriority.Low)]
    [DataRow("python:S1313", null)]
    [DataRow("cpp:S2245", HotspotPriority.Medium)]
    [DataRow("c:S2245", HotspotPriority.Medium)]
    [DataTestMethod]
    public void GetPriority_ShouldReturnAsExpected(string ruleKey, HotspotPriority? expectedPriority)
    {
        new HotspotReviewPriorityProvider().GetPriority(ruleKey).Should().Be(expectedPriority);
    }
    
    [TestMethod]
    public void GetPriority_NullKey_Throws()
    {
        Func<HotspotPriority?> f = () => new HotspotReviewPriorityProvider().GetPriority(null);
        f.Should().Throw<ArgumentNullException>();
    }
}
